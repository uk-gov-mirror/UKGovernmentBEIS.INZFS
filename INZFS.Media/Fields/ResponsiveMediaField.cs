using OrchardCore.ContentManagement;

namespace INZFS.Media.Fields
{
    public class ResponsiveMediaField : ContentField
    {
        public string Data { get; set; }

        public bool HasData
        {
            get { return !(string.IsNullOrWhiteSpace(Data) || Data == "[]"); }
        }
    }
}
