using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace X42.Controllers.Requests
{
    public class LogRulesRequest
    {
        public List<LogRuleRequest> LogRules { get; set; }
    }

    public class LogRuleRequest
    {
        /// <summary>
        ///     The name of the rule.
        /// </summary>
        [Required(ErrorMessage = "The name of the rule is missing.")]
        public string RuleName { get; set; }

        /// <summary>
        ///     The log level.
        /// </summary>
        [Required(ErrorMessage = "The log level is missing.")]
        public string LogLevel { get; set; }
    }
}