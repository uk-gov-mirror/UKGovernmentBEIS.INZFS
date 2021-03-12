using INZFS.Workflows.Activities;
using INZFS.Workflows.ViewModels;
using OrchardCore.Workflows.Display;
using OrchardCore.Workflows.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INZFS.Workflows.Drivers
{
    public class GovNotifyDisplayDriver : ActivityDisplayDriver<GovNotify, GovNotifyViewModel>
    {
          protected override void EditActivity(GovNotify activity, GovNotifyViewModel model)
        {
            model.NotificationType = activity.NotificationType;
            model.Message = activity.Message.Expression;
        }

        protected override void UpdateActivity(GovNotifyViewModel model, GovNotify activity)
        {
            activity.NotificationType = model.NotificationType;
            activity.Message = new WorkflowExpression<string>(model.Message);
        }
    }
}
