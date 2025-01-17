﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using nClam;
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
using System.IO;
using Microsoft.AspNetCore.Http;
using OrchardCore.Media;
using OrchardCore.FileStorage;
using Microsoft.Extensions.Logging;
using System.Globalization;
using INZFS.MVC.Drivers;
using System.Linq.Expressions;
using INZFS.MVC.Models.ProposalFinance;
using INZFS.MVC.ViewModels.ProposalFinance;

namespace INZFS.MVC.Controllers
{
    [Authorize]
    public class FundApplicationController : Controller
    {
        private const string contentType = "ProposalSummaryPart";

        private readonly IContentManager _contentManager;
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly IContentItemDisplayManager _contentItemDisplayManager;
        private readonly IHtmlLocalizer H;
        private readonly IMediaFileStore _mediaFileStore;
        private readonly dynamic New;
        private readonly INotifier _notifier;
        private readonly YesSql.ISession _session;
        private readonly ISiteService _siteService;
        private readonly IUpdateModelAccessor _updateModelAccessor;
        private readonly INavigation _navigation;
        private const string UploadedFileFolderRelativePath = "GovUpload/UploadedFiles";
        private string[] permittedExtensions = { ".txt", ".pdf", ".xls", ".xlsx", ".doc",".docx" };
        private readonly ILogger _logger;
        private readonly ClamClient _clam;

        public FundApplicationController(ILogger<FundApplicationController> logger, ClamClient clam, IContentManager contentManager, IMediaFileStore mediaFileStore, IContentDefinitionManager contentDefinitionManager,
            IContentItemDisplayManager contentItemDisplayManager, IHtmlLocalizer<FundApplicationController> htmlLocalizer,
            INotifier notifier, YesSql.ISession session, IShapeFactory shapeFactory, ISiteService siteService,
            IUpdateModelAccessor updateModelAccessor, INavigation navigation)
        {
            _contentManager = contentManager;
            _mediaFileStore = mediaFileStore;
            _contentDefinitionManager = contentDefinitionManager;
            _contentItemDisplayManager = contentItemDisplayManager;
            _notifier = notifier;
            _session = session;
            _siteService = siteService;
            _updateModelAccessor = updateModelAccessor;
            _clam = clam;
            _logger = logger;
            H = htmlLocalizer;
            New = shapeFactory;
            _navigation = navigation;
        }

        [HttpGet]
        public async Task<IActionResult> Section(string pagename, string id)
        {
            if(string.IsNullOrEmpty(pagename))
            {
                return NotFound();
            }
            pagename = pagename.ToLower().Trim();

            if (pagename == "application-summary")
            {
                var model = await GetApplicationSummaryModel();
                return View("ApplicationSummary", model);
            }

            if (pagename == "summary")
            {
                var model = await GetSummaryModel();
                return View("Summary", model);
            }
            if (pagename == "proposal-written-summary")
            {
                var model = await GetApplicationWrittenSummaryModel();
                return View("ProposalWrittenSummary", model);
            }
            if (pagename == "proposal-finance-summary")
            {
                var model = await GeProposalFinanceModel();
                return View("ProposalFinanceSummary", model);
            }
            
            var page = _navigation.GetPage(pagename);
            if (page == null)
            {
                return NotFound();
            }
            else
            {
                if(page is ViewPage)
                {
                    var viewPage = (ViewPage)page;
                    Expression<Func<ContentItemIndex, bool>> expression = index => index.ContentType == viewPage.ContentType;
                    var contentItems = await GetContentItems(expression);
                    var model = contentItems.Any() ? contentItems.First().As<ApplicationDocumentPart>(): new ApplicationDocumentPart();
                    ViewBag.ContentItemId = model.ContentItem?.ContentItemId;
                    return View(viewPage.ViewName, model);
                }
                
                return await Create(((ContentPage)page).ContentType);
            }
        }
        
        public async Task<bool> CreateDirectory(string directoryName)
        {
            if(directoryName == null)
            {
                return false;
            }
            await _mediaFileStore.TryCreateDirectoryAsync(directoryName);
            return true;
        }

        public async Task<IActionResult> Create(string contentType)
        {
            if (String.IsNullOrWhiteSpace(contentType))
            {
                return NotFound();
            }

            Expression<Func<ContentItemIndex, bool>> expression = index => index.ContentType == contentType;
            var contentItems = await GetContentItems(expression);

            if (contentItems.Any())
            {
                var existingContentItem = contentItems.First();
                return await Edit(existingContentItem.ContentItemId, contentType);
            }
            var newContentItem = await _contentManager.NewAsync(contentType);
            var model = await _contentItemDisplayManager.BuildEditorAsync(newContentItem, _updateModelAccessor.ModelUpdater, true);

            return View("Create", model);

        }

        [HttpPost, ActionName("Create")]
        [FormValueRequired("submit.Publish")]
        public async Task<IActionResult> CreateAndPublishPOST([Bind(Prefix = "submit.Publish")] string submitPublish, string returnUrl, string contentType, IFormFile? file)
        {
            var stayOnSamePage = submitPublish == "submit.PublishAndContinue";
            
            return await CreatePOST(contentType, returnUrl, stayOnSamePage, file, async contentItem =>
            {
                await _contentManager.PublishAsync(contentItem);

                var currentContentType = contentItem.ContentType;
            });
        }

        private async Task<IActionResult> CreatePOST(string id, string returnUrl, bool stayOnSamePage, IFormFile? file, Func<ContentItem, Task> conditionallyPublish)
        {
            var contentItem = await _contentManager.NewAsync(id);

            if (file != null)
            {
                var errorMessage = await Validate(file);
                var page = Request.Form["pagename"].ToString();
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    if(page == "ExperienceSkills" || page == "ProjectPlan")
                    {
                        ViewBag.ErrorMessage = errorMessage;
                        return View(page, new ApplicationDocumentPart());
                    }
                    ViewBag.ErrorMessage = errorMessage;
                    return View(page);
                }

                var publicUrl = await SaveFile(file, contentItem.ContentItemId);
                TempData["UploadDetail"] = new UploadDetail{
                    ContentItemProperty = Request.Form["contentTypeProperty"],
                    FileName = publicUrl
                };
            }

            contentItem.Owner = User.Identity.Name;

            var model = await _contentItemDisplayManager.UpdateEditorAsync(contentItem, _updateModelAccessor.ModelUpdater, true);

            if (!ModelState.IsValid)
            {
                _session.Cancel();
                return View("Create", model);
            }

            await _contentManager.CreateAsync(contentItem, VersionOptions.Draft);

            await conditionallyPublish(contentItem);

            var nextPageUrl = GetNextPageUrl(contentItem.ContentType);
            if (string.IsNullOrEmpty(nextPageUrl))
            {
                return NotFound();
            }
            return RedirectToAction("section", new { pagename = nextPageUrl });
        }

   
        private string ModifyFileName(string originalFileName)
        {
            DateTime thisDate = DateTime.UtcNow;
            CultureInfo culture = new CultureInfo("pt-BR");
            DateTimeFormatInfo dtfi = culture.DateTimeFormat;
            dtfi.DateSeparator = "-";
            var newDate = thisDate.ToString("d", culture);
            var newTime = thisDate.ToString("FFFFFF",culture);
            var newFileName = newDate + newTime + originalFileName;
            return newFileName;
        }
        private async Task<string> SaveFile(IFormFile file, string directoryName)
        {
            var DirectoryCreated = await CreateDirectory(directoryName);

            if (DirectoryCreated)
            {
                var newFileName = ModifyFileName(file.FileName);
                var mediaFilePath = _mediaFileStore.Combine(directoryName, newFileName);
                using (var stream = file.OpenReadStream())
                {
                    await _mediaFileStore.CreateFileFromStreamAsync(mediaFilePath, stream);
                }

                ViewBag.Message = "Upload Successful!";
                var publicUrl = _mediaFileStore.MapPathToPublicUrl(mediaFilePath);
                return publicUrl;
            }
            else
            {
                return string.Empty;
            }
           

          
        }
        private bool IsValidFileExtension(IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

            return string.IsNullOrEmpty(ext) || !permittedExtensions.Contains(ext);
        }

        private async Task<string> Validate(IFormFile file)
        {
            if (IsValidFileExtension(file))
            {
                return "Cannot accept files other than .doc, .docx, .xlx, .xlsx, .pdf";
            }
            if (file == null || file.Length == 0)
            {
                return "Empty file";
            }

            var notContainsVirus = true;//await ScanFile(file);
            if (!notContainsVirus)
            {
                return "File contains virus";
            }

            return string.Empty;
        }


        public async Task<bool> ScanFile(IFormFile file)
        {
            var log = new List<ScanResult>();
            if (file.Length > 0)
            {
                var extension = file.FileName.Contains('.')
                    ? file.FileName.Substring(file.FileName.LastIndexOf('.'), file.FileName.Length - file.FileName.LastIndexOf('.'))
                    : string.Empty;
                var newfile = new Models.File
                {
                    Name = $"{Guid.NewGuid()}{extension}",
                    Alias = file.FileName,
                    ContentType = file.ContentType,
                    Size = file.Length,
                    Uploaded = DateTime.UtcNow,
                };
                var ping = await _clam.PingAsync();

                if (ping)
                {
                    _logger.LogInformation("Successfully pinged the ClamAV server.");
                    var result = await _clam.SendAndScanFileAsync(file.OpenReadStream());

                    newfile.ScanResult = result.Result.ToString();
                    newfile.Infected = result.Result == ClamScanResults.VirusDetected;
                    newfile.Scanned = DateTime.UtcNow;
                    if (result.InfectedFiles != null)
                    {
                        foreach (var infectedFile in result.InfectedFiles)
                        {
                            newfile.Viruses.Add(new Virus
                            {
                                Name = infectedFile.VirusName
                            });
                        } return false;
                    }
                    else
                    {
                        var metaData = new Dictionary<string, string>
                        {
                            { "av-status", result.Result.ToString() },
                            { "av-timestamp", DateTime.UtcNow.ToString() },
                            { "alias", newfile.Alias }
                        };

                        var scanResult = new ScanResult()
                        {
                            FileName = file.FileName,
                            Result = result.Result.ToString(),
                            Message = result.InfectedFiles?.FirstOrDefault()?.VirusName,
                            RawResult = result.RawResult
                        };
                        log.Add(scanResult);
                    }
                    return true;
                }
                else
                {
                    _logger.LogWarning("Wasn't able to connect to the ClamAV server.");
                    return false;
                }

            }
            return false;
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string contentItemId, string contentName)
        {
            var contentItem = await _contentManager.GetAsync(contentItemId, VersionOptions.Latest);

            if (contentItem == null)
                return NotFound();

            var model = await _contentItemDisplayManager.BuildEditorAsync(contentItem, _updateModelAccessor.ModelUpdater, false);

            return View("Edit", model);
        }

        [HttpPost, ActionName("Edit")]
        [FormValueRequired("submit.Publish")]
        public async Task<IActionResult> EditAndPublishPOST(string contentItemId, [Bind(Prefix = "submit.Publish")] string submitPublish, string returnUrl, IFormFile? file)
        {
            var stayOnSamePage = submitPublish == "submit.PublishAndContinue";

            var content = await _contentManager.GetAsync(contentItemId, VersionOptions.Latest);

            if (content == null)
            {
                return NotFound();
            }
            if(file != null)
            {
                var errorMessage = await Validate(file);

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    ViewBag.ErrorMessage = errorMessage;
                    return await Edit(contentItemId,content.ContentType);
                }
                var publicUrl = await SaveFile(file, contentItemId);
                TempData["UploadDetail"] = new UploadDetail { 
                    ContentItemProperty = Request.Form["contentTypeProperty"],
                    FileName = publicUrl
                };
            }

            return await EditPOST(contentItemId, returnUrl, stayOnSamePage, async contentItem =>
            {
                await _contentManager.PublishAsync(contentItem);

                var typeDefinition = _contentDefinitionManager.GetTypeDefinition(contentItem.ContentType);

            });
        }

        private async Task<IActionResult> EditPOST(string contentItemId, string returnUrl, bool stayOnSamePage, Func<ContentItem, Task> conditionallyPublish)
        {
            var contentItem = await _contentManager.GetAsync(contentItemId, VersionOptions.DraftRequired);

            if (contentItem == null)
            {
                return NotFound();
            }

            var model = await _contentItemDisplayManager.UpdateEditorAsync(contentItem, _updateModelAccessor.ModelUpdater, false);
            if (!ModelState.IsValid)
            {
                _session.Cancel();
                return View("Edit", model);
            }


            _session.Save(contentItem);

            await conditionallyPublish(contentItem);

            var nextPageUrl = GetNextPageUrl(contentItem.ContentType);
            if (string.IsNullOrEmpty(nextPageUrl))
            {
                return NotFound();
            }
            return RedirectToAction("section", new { pagename = nextPageUrl });
        }

        public async Task<IActionResult> Remove(string contentItemId, string returnUrl)
        {
            var contentItem = await _contentManager.GetAsync(contentItemId, VersionOptions.Latest);

            if (contentItem != null)
            {
                var typeDefinition = _contentDefinitionManager.GetTypeDefinition(contentItem.ContentType);

                await _contentManager.RemoveAsync(contentItem);

                _notifier.Success(string.IsNullOrWhiteSpace(typeDefinition.DisplayName)
                    ? H["That content has been removed."]
                    : H["That {0} has been removed.", typeDefinition.DisplayName]);
            }

            return Url.IsLocalUrl(returnUrl) ? (IActionResult)LocalRedirect(returnUrl) : RedirectToAction("Index");
        }

        private async Task<SummaryViewModel> GetSummaryModel()
        {
            Expression<Func<ContentItemIndex, bool>> expression = x => x.ContentType == "ProjectSummaryPart"
                || x.ContentType == "ProjectDetailsPart"
                || x.ContentType == "OrgFundingPart";

            var items = await GetContentItems(expression);
            var projectSummaryPart = GetContentPart<ProjectSummaryPart>(items, "ProjectSummaryPart");
            var projectDetailsPart = GetContentPart<ProjectDetailsPart>(items, "ProjectDetailsPart");
            var fundingPart = GetContentPart<OrgFundingPart>(items, "OrgFundingPart");

            var model = new SummaryViewModel
            {
                ProjectSummaryViewModel = new ProjectSummaryViewModel
                {
                    ProjectName = projectSummaryPart.ProjectName,
                    Day = projectSummaryPart.Day,
                    Month = projectSummaryPart.Month,
                    Year = projectSummaryPart.Year,
                },
                ProjectDetailsViewModel = new ProjectDetailsViewModel
                {
                    Summary = projectDetailsPart.Summary,
                    Timing = projectDetailsPart.Timing
                },
                OrgFundingViewModel = new OrgFundingViewModel
                {
                    NoFunding = fundingPart.NoFunding,
                    Funders = fundingPart.Funders,
                    FriendsAndFamily = fundingPart.FriendsAndFamily,
                    PublicSectorGrants = fundingPart.PublicSectorGrants,
                    AngelInvestment = fundingPart.AngelInvestment,
                    VentureCapital = fundingPart.VentureCapital,
                    PrivateEquity = fundingPart.PrivateEquity,
                    StockMarketFlotation = fundingPart.StockMarketFlotation
                },
            };

            return model;
        }

        private async Task<ProposalFinanceSummaryViewModel> GeProposalFinanceModel()
        {
            Expression<Func<ContentItemIndex, bool>> expression = x => x.ContentType == "FinanceTurnover"
                || x.ContentType == "FinanceBalanceSheet"
                || x.ContentType == "FinanceRecoverVat"
                || x.ContentType == "FinanceBarriers";

            var items = await GetContentItems(expression);
            var financeTurnoverPart = GetContentPart<FinanceTurnoverPart>(items, "FinanceTurnover");
            var financeBalanceSheetPart = GetContentPart<FinanceBalanceSheetPart>(items, "FinanceBalanceSheet");
            var financeRecoverVatPart = GetContentPart<FinanceRecoverVatPart>(items, "FinanceRecoverVat");
            var financeBarriersPart = GetContentPart<FinanceBarriersPart>(items, "FinanceBarriers");

            var model = new ProposalFinanceSummaryViewModel
            {
                FinanceTurnoverViewModel = new FinanceTurnoverViewModel
                {
                    TurnoverAmount = financeTurnoverPart.TurnoverAmount,
                    Day = financeTurnoverPart.Day,
                    Month = financeTurnoverPart.Month,
                    Year = financeTurnoverPart.Year,
                },
                FinanceBalanceSheetViewModel = new FinanceBalanceSheetViewModel
                {
                    BalanceSheetTotal = financeBalanceSheetPart.BalanceSheetTotal,
                    Day = financeBalanceSheetPart.Day,
                    Month = financeBalanceSheetPart.Month,
                    Year = financeBalanceSheetPart.Year,
                },
                FinanceRecoverVatViewModel = new FinanceRecoverVatViewModel
                {
                    AbleToRecover = financeRecoverVatPart.AbleToRecover
                },
                FinanceBarriersViewModel = new FinanceBarriersViewModel
                {
                    Placeholder1 = financeBarriersPart.Placeholder1,
                    Placeholder2 = financeBarriersPart.Placeholder2,
                    Placeholder3 = financeBarriersPart.Placeholder3
                }
            };

            return model;
        }

        private async Task<ProposalWrittenSummaryViewModel> GetApplicationWrittenSummaryModel()
        {
            Expression<Func<ContentItemIndex, bool>> expression = x => x.ContentType == "ProjectProposalDetails"
                || x.ContentType == "ProjectExperience"
                || x.ContentType == "ApplicationDocument";

            var items = await GetContentItems(expression);

            var projectProposalPart = GetContentPart<ProjectProposalDetailsPart>(items, "ProjectProposalDetails");
            var projectExperiencePart = GetContentPart<ProjectExperiencePart>(items, "ProjectExperience");
            var applicationDocumentPart = GetContentPart<ApplicationDocumentPart>(items, "ApplicationDocument");

            var model = new ProposalWrittenSummaryViewModel()
            {
                ProjectProposalDetailsViewModel = new ProjectProposalDetailsViewModel
                {
                    InnovationImpactSummary = projectProposalPart.InnovationImpactSummary,
                    Day = projectProposalPart.Day,
                    Month = projectProposalPart.Month,
                    Year = projectProposalPart.Year,
                },
                ProjectExperienceViewModel = new ProjectExperienceViewModel
                {
                    ExperienceSummary = projectExperiencePart.ExperienceSummary,
                }
            };

            return model;
        }

        private string GetNextPageUrl(string contentType)
        {
            string nextPage = Request.Form["nextPage"].ToString();
            if(string.IsNullOrEmpty(nextPage))
            {
                var page = _navigation.GetNextPageByContentType(contentType);
                return page.Name;
            }

            return nextPage;
        }

        private async Task<IEnumerable<ContentItem>> GetContentItems(Expression<Func<ContentItemIndex, bool>> predicate)
        {
            var query = _session.Query<ContentItem, ContentItemIndex>();
            query = query.With<ContentItemIndex>(predicate);
            query = query.With<ContentItemIndex>(x => x.Published);
            query = query.With<ContentItemIndex>(x => x.Author == User.Identity.Name);

            return await query.ListAsync();
        }

        private async Task<ApplicationSummaryModel> GetApplicationSummaryModel()
        {
            Expression<Func<ContentItemIndex, bool>> expression = x => x.ContentType == "ProjectSummaryPart"
                || x.ContentType == "ProjectDetailsPart"
                || x.ContentType == "OrgFundingPart"
                || x.ContentType == "ProjectProposalDetails"
                || x.ContentType == "ProjectExperience"
                || x.ContentType == "ApplicationDocument"
                || x.ContentType == "FinanceTurnover"
                || x.ContentType == "FinanceBalanceSheet"
                || x.ContentType == "FinanceRecoverVat"
                || x.ContentType == "FinanceBarriers";

            var items = await GetContentItems(expression);

            var model = new ApplicationSummaryModel()
            {
                TotalSections = 11
            };

            UpdateModel<ProjectSummaryPart>(items, "ProjectSummaryPart", model, Sections.ProjectSummary);
            UpdateModel<ProjectDetailsPart>(items, "ProjectDetailsPart", model, Sections.ProjectDetails);
            UpdateModel<OrgFundingPart>(items, "OrgFundingPart", model, Sections.Funding);
            UpdateModel<ProjectProposalDetailsPart>(items, "ProjectProposalDetails", model, Sections.ProjectProposalDetails);
            UpdateModel<ProjectExperiencePart>(items, "ProjectExperience", model, Sections.ProjectExperience);

            var contentItem = items.FirstOrDefault(item => item.ContentType == "ApplicationDocument");
            var applicationDocumentPart = contentItem?.ContentItem.As<ApplicationDocumentPart>();
            if (applicationDocumentPart != null)
            {
                if(!string.IsNullOrEmpty(applicationDocumentPart.ProjectPlan))
                {
                    model.TotalCompletedSections++;
                    model.CompletedSections = model.CompletedSections | Sections.ProjectPlanUpload;
                }
                if (!string.IsNullOrEmpty(applicationDocumentPart.ExperienceAndSkills))
                {
                    model.TotalCompletedSections++;
                    model.CompletedSections = model.CompletedSections | Sections.ProjectExperienceSkillsUpload;
                }
            }

            UpdateModel<FinanceTurnoverPart>(items, "FinanceTurnover", model, Sections.FinanceTurnover);
            UpdateModel<FinanceBalanceSheetPart>(items, "FinanceBalanceSheet", model, Sections.FinanceBalanceSheet);
            UpdateModel<FinanceRecoverVatPart>(items, "FinanceRecoverVat", model, Sections.FinanceRecoverVat);
            UpdateModel<FinanceBarriersPart>(items, "FinanceBarriers", model, Sections.FinanceBarriers);

            return model;
        }


        private void UpdateModel<T>(IEnumerable<ContentItem> contentItems, string contentToFilter, ApplicationSummaryModel model, Sections section) where T : ContentPart
        {
            var contentItem = contentItems.FirstOrDefault(item => item.ContentType == contentToFilter);
            var contentPart = contentItem?.ContentItem.As<T>();
            
            if (contentPart != null)
            {
                model.TotalCompletedSections++;
                model.CompletedSections = model.CompletedSections | section;
            }
        }

        private T GetContentPart<T>(IEnumerable<ContentItem> contentItems, string contentToFilter) where T : ContentPart
        {
            var contentItem = contentItems.FirstOrDefault(item => item.ContentType == contentToFilter);
            return contentItem?.ContentItem.As<T>();
        }
    }
}
