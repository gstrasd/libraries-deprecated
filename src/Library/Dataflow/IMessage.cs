using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Library.Dataflow
{
    public interface IMessage
    {
        Guid CorrelationId { get; set; }
        string? MessageId { get; set; }
        string? Receipt { get; set; }
    }

    public abstract class QueueMessage : IMessage
    {
        [Required]
        [JsonPropertyName("correlationId")]
        public Guid CorrelationId { get; set; }

        [JsonIgnore]
        string? IMessage.MessageId { get; set; }

        [JsonIgnore]
        string? IMessage.Receipt { get; set; }
    }
}