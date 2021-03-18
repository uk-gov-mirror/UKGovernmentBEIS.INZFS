using INZFS.Media.Fields;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Metadata.Models;

namespace INZFS.Media.ViewModels
{
    public class EditResponsiveMediaFieldViewModel
    {
        public ResponsiveMediaField Field { get; set; }
        public ContentPart Part { get; set; }
        public ContentPartFieldDefinition PartFieldDefinition { get; set; }

        public string Data { get; set; }
    }
}
