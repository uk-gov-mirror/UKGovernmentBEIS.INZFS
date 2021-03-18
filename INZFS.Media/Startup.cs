using Fluid;
using Microsoft.Extensions.DependencyInjection;
using INZFS.Media.Drivers;
using INZFS.Media.Fields;
using INZFS.Media.Settings;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentTypes.Editors;
using OrchardCore.Modules;
using INZFS.Media.Models;
using INZFS.Media.ViewModels;
using OrchardCore.Data.Migration;

namespace INZFS.Media
{
    [Feature("INZFS.Media")]
    public class Startup : StartupBase
    {
        static Startup()
        {
            TemplateContext.GlobalMemberAccessStrategy.Register<DisplayResponsiveMediaFieldViewModel>();
            TemplateContext.GlobalMemberAccessStrategy.Register<ResponsiveMediaItem>();
            TemplateContext.GlobalMemberAccessStrategy.Register<ResponsiveMediaSource>();
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IDataMigration, Migrations>();

            services.AddContentField<ResponsiveMediaField>()
                .UseDisplayDriver<ResponsiveMediaFieldDisplayDriver>();

            services.AddScoped<IContentPartFieldDefinitionDisplayDriver, ResponsiveMediaFieldSettingsDriver>();
        }
    }
}
