using INZFS.Media.Fields;
using INZFS.Media.Settings;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.Data.Migration;

namespace INZFS.Media
{
    public class Migrations : DataMigration
    {
        #region Dependencies

        private readonly IContentDefinitionManager _contentDefinitionManager;

        #endregion Dependencies

        #region Constructor

        public Migrations(IContentDefinitionManager contentDefinitionManager)
        {
            _contentDefinitionManager = contentDefinitionManager;
        }

        #endregion Constructor

        #region Migrations

        public int Create()
        {
            _contentDefinitionManager.MigrateFieldSettings<ResponsiveMediaField, ResponsiveMediaFieldSettings>();

            return 1;
        }

        #endregion Migrations
    }
}
