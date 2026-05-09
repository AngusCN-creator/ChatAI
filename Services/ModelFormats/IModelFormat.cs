using System;
using System.Collections.Generic;

namespace ChatAI.Services.ModelFormats
{
    /// <summary>
    /// 模型格式接口（IModelFormat）
    /// 
    /// 该接口定义了不同AI模型的请求格式规范，包括Prompt构建、停止词获取和响应清理等功能。
    /// 通过实现此接口，可以支持多种不同格式的大语言模型（如Qwen、Hermes等），实现模型的灵活切换。
    /// 
    /// 设计目的：
    /// 1. 统一不同模型的交互接口，使上层代码无需关心具体模型格式
    /// 2. 支持模型格式的扩展，新增模型只需实现此接口即可
    /// 3. 解耦模型格式与业务逻辑，提高代码可维护性
    /// </summary>
    public interface IModelFormat
    {
        /// <summary>
        /// 获取模型名称标识
        /// </summary>
        /// <value>模型名称，如 "Qwen"、"Hermes" 等</value>
        string ModelName { get; }

        /// <summary>
        /// 构建聊天请求的Prompt字符串
        /// 
        /// 根据模型格式要求，将系统提示词、对话历史和当前用户消息组合成完整的Prompt。
        /// 不同模型有不同的格式要求（如ChatML、Llama-3格式等）。
        /// </summary>
        /// <param name="systemContent">系统提示词，定义AI的行为和角色</param>
        /// <param name="conversationHistory">对话历史列表，每个元素包含角色(role)和内容(content)</param>
        /// <param name="userMessage">用户当前输入的消息</param>
        /// <returns>格式化后的完整Prompt字符串</returns>
        string BuildChatPrompt(string systemContent, List<(string role, string content)> conversationHistory, string userMessage);

        /// <summary>
        /// 构建记忆生成请求的Prompt字符串
        /// 
        /// 与聊天Prompt不同，记忆生成Prompt用于让模型总结对话历史生成记忆内容。
        /// </summary>
        /// <param name="systemContent">系统提示词，指导记忆生成的规则</param>
        /// <param name="conversationHistory">需要总结的对话历史文本</param>
        /// <returns>格式化后的记忆生成Prompt字符串</returns>
        string BuildMemoryPrompt(string systemContent, string conversationHistory);

        /// <summary>
        /// 获取模型的停止词列表
        /// 
        /// 停止词用于告诉模型何时停止生成文本。不同模型有不同的停止词定义。
        /// 例如Qwen使用 &lt;|im_end|&gt;，Llama-3使用 &lt;|eot_id|&gt; 等。
        /// </summary>
        /// <returns>停止词字符串数组</returns>
        string[] GetStopWords();

        /// <summary>
        /// 清理模型返回的原始响应
        /// 
        /// 模型返回的响应可能包含停止词、特殊标记或重复内容，此方法负责清理这些内容，
        /// 返回干净的纯文本响应。
        /// </summary>
        /// <param name="response">模型返回的原始响应字符串</param>
        /// <returns>清理后的纯文本响应</returns>
        string CleanResponse(string response);
    }
}
