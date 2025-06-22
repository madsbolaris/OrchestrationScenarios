// <copyright file="Agent.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

#nullable enable

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OrchestrationScenarios.Models.Agents;

[JsonConverter(typeof(BaseAgentConverter<Agent>))]
public class Agent : BaseAgent
{
    private string? _agentId;

    public string AgentId
    {
        get
        {
            if (!string.IsNullOrEmpty(_agentId))
            {
                return _agentId;
            }

            _agentId = ComputeHash();
            return _agentId;
        }
        set => _agentId = value;
    }

    private string ComputeHash()
    {
        var hashInput = new
        {
            DisplayName,
            Model,
            Instructions,
            Tools,
            ToolChoice,
            Metadata
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(hashInput, options);
        var bytes = Encoding.UTF8.GetBytes(json);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes);
    }
}
