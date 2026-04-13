using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models
{
    public class LogMessage(string logLevel, string loggerName, string message)
    {
        [Key]
        public Guid LogId { get; set; }

        //Time of log
        [Column(TypeName = "timestamp without time zone")]
        public DateTime TimeStamp { get; set; } = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila"));

        //Severity level: Information, Warning, Error, etc.
        public string LogLevel { get; set; } = logLevel;

        //Name where the log comes
        public string LoggerName { get; set; } = loggerName;

        //Log description
        public string Message { get; set; } = message;
    }
}
