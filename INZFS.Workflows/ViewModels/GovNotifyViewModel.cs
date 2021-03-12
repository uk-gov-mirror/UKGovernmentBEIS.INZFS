using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using OrchardCore.DisplayManagement.Notify;

namespace INZFS.Workflows.ViewModels
{
    public class GovNotifyViewModel
    {
        public NotifyType NotificationType { get; set; }

        [Required]
        public string Message { get; set; }
    }
}



