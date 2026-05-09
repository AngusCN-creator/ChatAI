using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ChatAI.Services.ModelFormats
{
    /// <summary>
    /// Qwen模型格式实现（QwenModelFormat）
    /// 
    /// Qwen（通义千问）是阿里云研发的大语言模型，使用ChatML格式：
    /// &lt;|im_start|&gt;role\ncontent&lt;|im_end|&gt;
    /// 
    /// 关键特点：
    /// 1. 使用标准ChatML格式，与OpenAI API兼容
    /// 2. 停止词使用 &lt;|im_end|&gt; 和 &lt;|im_start|&gt;
    /// 3. 支持多轮对话和记忆生成
    /// </summary>
    public class QwenModelFormat : IModelFormat
    {
        /// <summary>
        /// 获取模型名称标识
        /// </summary>
        public string ModelName => "Qwen";

        /// <summary>
        /// ChatML格式开始标记
        /// </summary>
        private const string ImStart = "<|im_start|>";

        /// <summary>
        /// ChatML格式结束标记
        /// </summary>
        private const string ImEnd = "<|im_end|>";

        /// <summary>
        /// 构建聊天请求的Prompt字符串
        /// 
        /// Qwen模型使用ChatML格式构建Prompt：
        /// &lt;|im_start|&gt;system\n系统提示词&lt;|im_end|&gt;
        /// &lt;|im_start|&gt;user\n用户消息1&lt;|im_end|&gt;
        /// &lt;|im_start|&gt;assistant\n助手回复1&lt;|im_end|&gt;
        /// ...
        /// &lt;|im_start|&gt;user\n当前用户消息&lt;|im_end|&gt;
        /// &lt;|im_start|&gt;assistant\n
        /// </summary>
        /// <param name="systemContent">系统提示词</param>
        /// <param name="conversationHistory">对话历史列表</param>
        /// <param name="userMessage">用户当前消息</param>
        /// <returns>格式化后的Prompt字符串</returns>
        public string BuildChatPrompt(string systemContent, List<(string role, string content)> conversationHistory, string userMessage)
        {
            var promptBuilder = new System.Text.StringBuilder();

            // 添加系统消息（如果有）
            if (!string.IsNullOrWhiteSpace(systemContent))
            {
                promptBuilder.AppendLine($"{ImStart}system\n{systemContent.Trim()}{ImEnd}");
            }

            // 添加对话历史消息
            if (conversationHistory != null)
            {
                foreach (var (role, content) in conversationHistory)
                {
                    // 将角色转换为模型期望的格式
                    string formattedRole = role.ToLower() switch
                    {
                        "user" => "user",
                        "assistant" => "assistant",
                        _ => "user"  // 默认视为用户消息
                    };
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        promptBuilder.AppendLine($"{ImStart}{formattedRole}\n{content.Trim()}{ImEnd}");
                    }
                }
            }

            // 添加用户当前消息
            if (!string.IsNullOrWhiteSpace(userMessage))
            {
                promptBuilder.AppendLine($"{ImStart}user\n{userMessage.Trim()}{ImEnd}");
            }

            // 添加assistant标记（模型从这里开始生成）
            promptBuilder.Append($"{ImStart}assistant\n");

            return promptBuilder.ToString();
        }

        /// <summary>
        /// 构建记忆生成请求的Prompt字符串
        /// </summary>
        /// <param name="systemContent">系统提示词</param>
        /// <param name="conversationHistory">对话历史文本</param>
        /// <returns>格式化后的Prompt字符串</returns>
        public string BuildMemoryPrompt(string systemContent, string conversationHistory)
        {
            var promptBuilder = new System.Text.StringBuilder();

            // 添加系统消息
            if (!string.IsNullOrWhiteSpace(systemContent))
            {
                promptBuilder.AppendLine($"{ImStart}system\n{systemContent.Trim()}{ImEnd}");
            }

            // 添加对话历史（作为用户消息）
            if (!string.IsNullOrWhiteSpace(conversationHistory))
            {
                promptBuilder.AppendLine($"{ImStart}user\n{conversationHistory.Trim()}{ImEnd}");
            }

            // 添加assistant标记（模型从这里开始生成）
            promptBuilder.Append($"{ImStart}assistant\n");

            return promptBuilder.ToString();
        }

        /// <summary>
        /// 获取模型的停止词列表
        /// 
        /// Qwen模型仅使用ChatML格式的停止词：
        /// - &lt;|im_end|&gt;：消息结束标记
        /// - &lt;|im_start|&gt;：消息开始标记（用于防止生成新消息）
        /// </summary>
        /// <returns>停止词数组</returns>
        public string[] GetStopWords()
        {
            return new[]
            {
                ImEnd,    // ChatML消息结束标记
                ImStart   // ChatML消息开始标记
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

            // 定义所有需要移除的停止词和特殊标记
            var allStopWords = new[]
            {
                ImEnd,           // ChatML消息结束标记
                ImStart,         // ChatML消息开始标记
                "\nUser:",       // 用户前缀（某些模型可能生成）
                "\nHuman:",      // 人类前缀
                "\n### Instruction:", // 指令前缀
                "[PAD",          // PAD标记
                "</s>",          // EOS标记
                "User:",         // 用户前缀（无换行）
                "Human:",        // 人类前缀（无换行）
                "'t be evil",    // 常见垃圾内容
                "Don't be evil"  // 常见垃圾内容
            };
            
            // 移除停止词及之后的内容
            foreach (var stopWord in allStopWords)
            {
                int stopIndex = response.IndexOf(stopWord, StringComparison.Ordinal);
                if (stopIndex >= 0)
                {
                    response = response.Substring(0, stopIndex).Trim();
                }
            }

            // 移除重复内容
            response = RemoveRepeatedContent(response);

            return response.Trim();
        }

        /// <summary>
        /// 移除重复内容
        /// </summary>
        /// <param name="response">原始响应</param>
        /// <returns>去重后的响应</returns>
        private string RemoveRepeatedContent(string response)
        {
            // 检测并移除重复的句子
            response = RemoveRepeatedSentences(response);
            
            // 检测并移除重复模式
            response = RemoveRepeatedPatterns(response);

            return response;
        }

        /// <summary>
        /// 移除重复的句子
        /// 
        /// 如果发现连续重复3次以上的句子，只保留前3次。
        /// </summary>
        /// <param name="text">原始文本</param>
        /// <returns>去重后的文本</returns>
        private string RemoveRepeatedSentences(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            // 按中文句号、感叹号、问号分割句子
            var sentences = Regex.Split(text, @"([。！？])").Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            var result = new System.Text.StringBuilder();
            string prevSentence = string.Empty;
            int repeatCount = 0;

            foreach (var sentence in sentences)
            {
                if (sentence.Equals(prevSentence, StringComparison.OrdinalIgnoreCase))
                {
                    repeatCount++;
                    if (repeatCount < 3)
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
