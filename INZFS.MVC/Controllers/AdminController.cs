using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Routing;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Records;
using INZFS.MVC.ViewModels;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Navigation;
using OrchardCore.Routing;
using OrchardCore.Settings;
using YesSql;
using Microsoft.AspNetCore.Authorization;
using INZFS.MVC.Models;
using INZFS.MVC.Forms;
using INZFS.MVC.Models.ProposalWritten;
using INZFS.MVC.ViewModels.ProposalWritten;
namespace INZFS.MVC.Controllers
{
    public class AdminController : Controller
    {

        private readonly IContentManager _contentManager;
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly IContentItemDisplayManager _contentItemDisplayManager;
        private readonly IHtmlLocalizer H;
        private readonly dynamic New;
        private readonly INotifier _notifier;
        private readonly ISession _session;
        private readonly ISiteService _siteService;
        private readonly IUpdateModelAccessor _updateModelAccessor;
        private readonly INavigation _navigation;

        public AdminController(IContentManager contentManager, IContentDefinitionManager contentDefinitionManager,
            IContentItemDisplayManager contentItemDisplayManager, IHtmlLocalizer<FundApplicationController> htmlLocalizer,
            INotifier notifier, ISession session, IShapeFactory shapeFactory, ISiteService siteService,
            IUpdateModelAccessor updateModelAccessor, INavigation navigation)
        {
            _contentManager = contentManager;
            _contentDefinitionManager = contentDefinitionManager;
            _contentItemDisplayManager = contentItemDisplayManager;
            _notifier = notifier;
            _session = session;
            _siteService = siteService;
            _updateModelAccessor = updateModelAccessor;

            H = htmlLocalizer;
            New = shapeFactory;
            _navigation = navigation;
        }

        public ActionResult ChildOne()
        {
            var model = GetAllApplications().Result;
            return View(model);
        }

        public ActionResult ChildTwo()
        {
            return View();
        }
        
        public async Task<ApplicationsSummaryModel> GetAllApplications()
        {
            var query = _session.Query<ContentItem, ContentItemIndex>();
            query = query.With<ContentItemIndex>(x => x.ContentType == "CompanyDetails");
            query = query.With<ContentItemIndex>(x => x.Published);
           
            var items = await query.ListAsync();

            var companyLists = items.Select(x => x.As<CompanyDetailsPart>()).ToList();



            var model = new ApplicationsSummaryModel
            { CompanyDetails = companyLists
            };



            return model;
          
        }




    }




}




