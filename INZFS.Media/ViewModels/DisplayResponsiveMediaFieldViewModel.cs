﻿using INZFS.Media.Fields;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Metadata.Models;
using System.Collections.Generic;
using System.Linq;

namespace INZFS.Media.ViewModels
{
    public class DisplayResponsiveMediaFieldViewModel
    {
        public ResponsiveMediaField Field { get; set; }
        public ContentPart Part { get; set; }
        public ContentPartFieldDefinition PartFieldDefinition { get; set; }

        public IList<Models.ResponsiveMediaItem> Media { get; set; }

        public bool HasMedia
        {
            get { return Media.Any(); }
        }
    }
}