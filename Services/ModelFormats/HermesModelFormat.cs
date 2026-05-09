using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ChatAI.Services.ModelFormats
{
    /// <summary>
    /// OpenAI格式消息类
    /// 
    /// 用于构建符合OpenAI API格式的消息对象，包含角色(role)和内容(content)。
    /// Hermes-2-Pro模型基于Llama-3，使用类似的消息格式。
    /// </summary>
    public class OpenAiMessage
    {
        /// <summary>
        /// 消息角色：system、user、assistant
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// 消息内容
        /// </summary>
        public string Content { get; set; }
    }

    /// <summary>
    /// Hermes模型格式实现（HermesModelFormat）
    /// 
    /// Hermes-2-Pro-Llama-3-8B 是基于 Meta Llama-3 8B 模型进行微调的开源模型，
    /// 使用Llama-3的prompt格式：&lt;|begin_of_text|&gt;&lt;|start_header_id|&gt;role&lt;|end_header_id|&gt;\n\ncontent&lt;|eot_id|&gt;
    /// 
    /// 关键特点：
    /// 1. 使用Llama-3原生格式，保持与官方规范一致
    /// 2. 严格控制停止词，多余的停止词会导致模型提前截断
    /// 3. 响应清理逻辑针对Llama-3格式进行优化
    /// </summary>
    public class HermesModelFormat : IModelFormat
    {
        /// <summary>
        /// 获取模型名称标识
        /// </summary>
        public string ModelName => "Hermes";

        /// <summary>
        /// 构建聊天请求的Prompt字符串
        /// 
        /// Hermes模型使用Llama-3格式构建Prompt：
        /// &lt;|begin_of_text|&gt;
        /// &lt;|start_header_id|&gt;system&lt;|end_header_id|&gt;\n\n系统提示词&lt;|eot_id|&gt;
        /// &lt;|start_header_id|&gt;user&lt;|end_header_id|&gt;\n\n用户消息1&lt;|eot_id|&gt;
        /// &lt;|start_header_id|&gt;assistant&lt;|end_header_id|&gt;\n\n助手回复1&lt;|eot_id|&gt;
        /// ...
        /// &lt;|start_header_id|&gt;user&lt;|end_header_id|&gt;\n\n当前用户消息&lt;|eot_id|&gt;
        /// &lt;|start_header_id|&gt;assistant&lt;|end_header_id|&gt;\n\n
        /// </summary>
        /// <param name="systemContent">系统提示词</param>
        /// <param name="conversationHistory">对话历史列表</param>
        /// <param name="userMessage">用户当前消息</param>
        /// <returns>格式化后的Prompt字符串</returns>
        public string BuildChatPrompt(string systemContent, List<(string role, string content)> conversationHistory, string userMessage)
        {
            var messages = new List<OpenAiMessage>();

            // 添加系统消息（如果有）
            if (!string.IsNullOrWhiteSpace(systemContent))
            {
                messages.Add(new OpenAiMessage { Role = "system", Content = systemContent.Trim() });
            }

            // 添加对话历史消息
            if (conversationHistory != null)
            {
                foreach (var (role, content) in conversationHistory)
                {
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        // 将角色转换为模型期望的格式
                        string formattedRole = role.ToLower() switch
                        {
                            "user" => "user",
                            "assistant" => "assistant",
                            _ => "user"  // 默认视为用户消息
                        };
                        messages.Add(new OpenAiMessage { Role = formattedRole, Content = content.Trim() });
                    }
                }
            }

            // 添加用户当前消息
            if (!string.IsNullOrWhiteSpace(userMessage))
            {
                messages.Add(new OpenAiMessage { Role = "user", Content = userMessage.Trim() });
            }

            // 转换为Llama-3格式的Prompt
            return ConvertOpenAiMessagesToPrompt(messages);
        }

        /// <summary>
        /// 构建记忆生成请求的Prompt字符串
        /// </summary>
        /// <param name="systemContent">系统提示词</param>
        /// <param name="conversationHistory">对话历史文本</param>
        /// <returns>格式化后的Prompt字符串</returns>
        public string BuildMemoryPrompt(string systemContent, string conversationHistory)
        {
            var messages = new List<OpenAiMessage>();

            // 添加系统消息
            if (!string.IsNullOrWhiteSpace(systemContent))
            {
                messages.Add(new OpenAiMessage { Role = "system", Content = systemContent.Trim() });
            }

            // 添加对话历史（作为用户消息）
            if (!string.IsNullOrWhiteSpace(conversationHistory))
            {
                messages.Add(new OpenAiMessage { Role = "user", Content = conversationHistory.Trim() });
            }

            // 转换为Llama-3格式
            return ConvertOpenAiMessagesToPrompt(messages);
        }

        /// <summary>
        /// 将OpenAI格式消息列表转换为Llama-3格式的Prompt
        /// 
        /// 格式说明：
        /// - 以 &lt;|begin_of_text|&gt; 开头
        /// - 每条消息格式：&lt;|start_header_id|&gt;role&lt;|end_header_id|&gt;\n\ncontent\n\n&lt;|eot_id|&gt;
        /// - 以 &lt;|start_header_id|&gt;assistant&lt;|end_header_id|&gt;\n\n 结尾（模型从此处开始生成）
        /// </summary>
        /// <param name="messages">OpenAI格式消息列表</param>
        /// <returns>Llama-3格式的Prompt字符串</returns>
        private string ConvertOpenAiMessagesToPrompt(List<OpenAiMessage> messages)
        {
            var promptBuilder = new StringBuilder();
            
            // Llama-3格式必须以 <|begin_of_text|> 开头
            promptBuilder.Append("<|begin_of_text|>");

            foreach (var msg in messages)
            {
                // 根据角色获取对应的标签
                string role = msg.Role switch
                {
                    "user" => "<|start_header_id|>user<|end_header_id|>",
                    "assistant" => "<|start_header_id|>assistant<|end_header_id|>",
                    "system" => "<|start_header_id|>system<|end_header_id|>",
                    _ => "<|start_header_id|>user<|end_header_id|>"
                };

                promptBuilder.Append(role);
                promptBuilder.Append("\n\n");
                promptBuilder.Append(msg.Content.Trim());
                promptBuilder.Append("\n\n<|eot_id|>");
            }

            // 添加assistant标签，模型从这里开始生成响应
            promptBuilder.Append("<|start_header_id|>assistant<|end_header_id|>\n\n");
            return promptBuilder.ToString();
        }

        /// <summary>
        /// 获取模型的停止词列表
        /// 
        /// 重要提示：Hermes-2-Pro-Llama-3 只使用官方停止词，多一个都不行！
        /// 模型刚生成就被多余的stop词截断会导致空回复。
        /// </summary>
        /// <returns>停止词数组</returns>
        public string[] GetStopWords()
        {
            return new[]
            {
                "<|eot_id|>",      // Llama-3官方停止词（end of turn）
                "<|end_of_text|>"   // Llama-3官方停止词（end of text）
            };
        }

        /// <summary>
        /// 清理模型返回的原始响应
        /// 
        /// 移除停止词、特殊标记、重复内容等，返回干净的纯文本响应。
        /// </summary>
        /// <param name="response">模型返回的原始响应</param>
        /// <returns>清理后的纯文本响应</returns>
        public string CleanResponse(string response)
        {
            if (string.IsNullOrEmpty(response))
                return response;

            // 移除各种特殊标记
            response = response.Replace("</s>", "");
            response = response.Replace("<|end_of_text|>", "");
            response = response.Replace("<|eot_id|>", "");
            response = response.Replace("<|begin_of_text|>", "");
            response = response.Replace("<|start_header_id|>", "");
            response = response.Replace("<|end_header_id|>", "");

            // 移除常见的前缀干扰
            response = Regex.Replace(response, @"^\s*(User:|Human:|### Instruction:|Assistant:|### Response:)\s*", "", RegexOptions.Multiline);

            // 合并多余的空行
            response = response.Replace("\n\n\n", "\n\n");

            // 移除尾部垃圾内容
            response = TrimTrailingGarbage(response);

            // 移除重复内容
            response = RemoveDuplicateContent(response);

            return response.Trim();
        }

        /// <summary>
        /// 移除响应尾部的垃圾内容
        /// 
        /// 某些模型可能会生成一些无意义的尾部内容，如 "'t be evil"、"Don't be evil"、"[PAD" 等。
        /// </summary>
        /// <param name="response">原始响应</param>
        /// <returns>清理后的响应</returns>
        private string TrimTrailingGarbage(string response)
        {
            string[] garbagePatterns = { "'t be evil", "Don't be evil", "[PAD", "###" };
            foreach (var pattern in garbagePatterns)
            {
                int index = response.IndexOf(pattern, StringComparison.Ordinal);
                if (index >= 0)
                {
                    response = response.Substring(0, index);
                }
            }
            return response;
        }

        /// <summary>
        /// 移除重复内容
        /// </summary>
        /// <param name="response">原始响应</param>
        /// <returns>清理后的响应</returns>
        private string RemoveDuplicateContent(string response)
        {
            response = RemoveRepeatedSentences(response);
            response = RemoveRepeatedPatterns(response);
            return response;
        }

        /// <summary>
        /// 移除重复的句子
        /// 
        /// Hermes-2生成重复内容的概率较低，将重复阈值从3次放宽到5次。
        /// </summary>
        /// <param name="text">原始文本</param>
        /// <returns>去重后的文本</returns>
        private string RemoveRepeatedSentences(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            // 按中文句号、感叹号、问号分割句子
            var sentences = Regex.Split(text, @"([。！？])").Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            var result = new StringBuilder();
            string prevSentence = string.Empty;
            int repeatCount = 0;

            // Hermes-2生成重复内容的概率较低，将重复阈值从3次放宽到5次
            const int repeatThreshold = 5;

            foreach (var sentence in sentences)
            {
                if (sentence.Equals(prevSentence, StringComparison.OrdinalIgnoreCase))
                {
                    repeatCount++;
                    if (repeatCount < repeatThreshold)
                    {
                        result.Append(sentence);
                    }
                }
                else
                {
                    result.Append(sentence);
                    prevSentence = sentence;
                    repeatCount = 0;
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// 检测并移除重复的模式
        /// 
        /// 如果发现连续重复3次的模式（长度5-100字符），则截断到第一次出现的位置。
        /// </summary>
        /// <param name="text">原始文本</param>
        /// <returns>去重后的文本</returns>
        private string RemoveRepeatedPatterns(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            // 最大模式长度不超过文本长度的一半
            int maxPatternLength = Math.Min(100, text.Length / 2);

            // 检查长度从5到maxPatternLength的所有模式
            for (int patternLength = 5; patternLength <= maxPatternLength; patternLength++)
            {
                for (int i = 0; i <= text.Length - patternLength * 3; i++)
                {
                    string pattern = text.Substring(i, patternLength);
                    int nextPos = i + patternLength;

                    // 检查是否连续出现3次相同模式
                    if (nextPos + patternLength <= text.Length &&
                        text.Substring(nextPos, patternLength) == pattern)
                    {
                        int thirdPos = nextPos + patternLength;
                        if (thirdPos + patternLength <= text.Length &&
                            text.Substring(thirdPos, patternLength) == pattern)
                        {
                            // 找到重复3次的模式，截断到第一次出现的位置
                            return text.Substring(0, i + patternLength);
                        }
                    }
                }
            }

            return text;
        }
    }
}
