using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Workflows.Abstractions.Models;
using OrchardCore.Workflows.Activities;
using OrchardCore.Workflows.Models;
using OrchardCore.Workflows.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace INZFS.Workflows.Activities
{
    public class GovNotify : TaskActivity
    {
        private readonly INotifier _notifier;
        private readonly IWorkflowExpressionEvaluator _expressionEvaluator;
        private readonly IStringLocalizer S;
        private readonly HtmlEncoder _htmlEncoder;

        public GovNotify(
            INotifier notifier,
            IWorkflowExpressionEvaluator expressionvaluator,
            IStringLocalizer<NotifyTask> localizer,
            HtmlEncoder htmlEncoder)
        {
            _notifier = notifier;
            _expressionEvaluator = expressionvaluator;
            S = localizer;
            _htmlEncoder = htmlEncoder;
        }

        public override string Name => nameof(GovNotify);

        public override LocalizedString DisplayText => S["Gov Notify Activity"];

        public override LocalizedString Category => S["UI"];

        public NotifyType NotificationType
        {
            get => GetProperty<NotifyType>();
            set => SetProperty(value);
        }

        public WorkflowExpression<string> Message
        {
            get => GetProperty(() => new WorkflowExpression<string>());
            set => SetProperty(value);
        }

        public override IEnumerable<Outcome> GetPossibleOutcomes(WorkflowExecutionContext workflowContext, ActivityContext activityContext)
        {
            return Outcomes(S["Done"]);
        }

        public override async Task<ActivityExecutionResult> ExecuteAsync(WorkflowExecutionContext workflowContext, ActivityContext activityContext)
        {
            var message = await _expressionEvaluator.EvaluateAsync(Message, workflowContext, _htmlEncoder);
            _notifier.Add(NotificationType, new LocalizedHtmlString(nameof(GovNotify), message));

            return Outcomes("Done");
        }
    }
}
