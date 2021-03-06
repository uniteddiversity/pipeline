﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Roadkill.Core.Converters;
using Roadkill.Core.Localization;
using Roadkill.Core.Configuration;
using System.Diagnostics;
using System.Web;
using System.Web.UI;
using Roadkill.Core.Services;
using Roadkill.Core.Security;
using Roadkill.Core.Mvc.Attributes;
using Roadkill.Core.Mvc.ViewModels;
using Roadkill.Core.Text;
using Roadkill.Core.Database;
using System.Text;
using Roadkill.Core.Database.LightSpeed;
using Roadkill.Core.Mvc.Controllers.Api;
using System.Xml.Serialization;
using System.Text.RegularExpressions;
using System.IO;
using System.Configuration;
using System.Net.Http;
using System.Web.Configuration;

namespace Roadkill.Core.Mvc.Controllers
{
	/// <summary>
	/// Provides functionality that is common through the site.
	/// </summary>
	[OptionalAuthorization]
	public class HomeController : ControllerBase
	{
		public PageService PageService { get; private set; }
		private SearchService _searchService;
		private MarkupConverter _markupConverter;

		public HomeController(ApplicationSettings settings, UserServiceBase userManager, MarkupConverter markupConverter,
			PageService pageService, SearchService searchService, IUserContext context, SettingsService settingsService)
			: base(settings, userManager, context, settingsService) 
		{
			_markupConverter = markupConverter;
			_searchService = searchService;
			PageService = pageService;
		}

		/// <summary>
		/// Display the homepage/mainpage. If no page has been tagged with the 'homepage' tag,
		/// then a dummy PageViewModel is put in its place.
		/// </summary>
		[BrowserCache]
		public ActionResult Index()
		{
			// Get the first locked homepage
			PageViewModel model = PageService.FindHomePage();

			if (model == null)
			{
				model = new PageViewModel();
				model.Title = SiteStrings.NoMainPage_Title;
				model.Content = SiteStrings.NoMainPage_Label;
				model.ContentAsHtml = _markupConverter.ToHtml(SiteStrings.NoMainPage_Label).Html;
				model.CreatedBy = "";
				model.CreatedOn = DateTime.UtcNow;
				model.RawTags = "homepage";
				model.ModifiedOn = DateTime.UtcNow;
				model.ModifiedBy = "";
			}

			return View(model);
		}

		/// <summary>
		/// Searches the lucene index using the search string provided.
		/// </summary>
        public ActionResult Search(string q, string language, string status, string startdatestart, string startdateend, string enddatestart, string enddateend)
		{
            if (String.IsNullOrWhiteSpace(q))
            {
                return View();
            }

			ViewData["search"] = q;

            q = "title:" + q + " OR content:" + q;

            if (!String.IsNullOrWhiteSpace(language) && language != "all")
            {
                q += " AND projectlanguage:" + language;
            }

            if (!String.IsNullOrWhiteSpace(status) && status != "all")
            {
                q += " AND projectstatus:" + status;
            }

            string strDefaultFromDate = "20340101";
            string strDefaultToDate = "20340101";

            if (!String.IsNullOrWhiteSpace(startdatestart) || !String.IsNullOrWhiteSpace(startdateend))
            {
                string strUseFromDate = strDefaultFromDate;
                string strUseEndDate = strDefaultToDate;

                if (!String.IsNullOrWhiteSpace(startdatestart))
                {
                    strUseFromDate = startdatestart.Replace("/", "");
                }

                if (!String.IsNullOrWhiteSpace(startdateend))
                {
                    strUseEndDate = startdateend.Replace("/", "");
                }

                q += " AND projectstart:[" + strUseFromDate + " TO " + strUseEndDate + "]";

            }

            if (!String.IsNullOrWhiteSpace(enddatestart) || !String.IsNullOrWhiteSpace(enddateend))
            {
                string strUseFromDate = strDefaultFromDate;
                string strUseEndDate = strDefaultToDate;

                if (!String.IsNullOrWhiteSpace(enddatestart))
                {
                    strUseFromDate = enddatestart.Replace("/", "");
                }

                if (!String.IsNullOrWhiteSpace(enddateend))
                {
                    strUseEndDate = enddateend.Replace("/", "");
                }

                q += " AND projectend:[" + strUseFromDate + " TO " + strUseEndDate + "]";

            }

			List<SearchResultViewModel> results = _searchService.Search(q).ToList();
			return View(results);
		}

		/// <summary>
		/// Returns Javascript 'constants' for the site.
		/// </summary>
		/// <param name="version">This is sent by the views to ensure new versions of Roadkill have this JS file cleared from the cache.</param>
		[CacheContentType(Duration = 86400 * 30, ContentType = "application/javascript")] // 30 days
		[AllowAnonymous]
		public ActionResult GlobalJsVars(string version)
		{
			return View();
		}

		/// <summary>
		/// Displays the left side menu view, including new page/settings if logged in.
		/// </summary>
		[AllowAnonymous]
		public ActionResult NavMenu()
		{
			return Content(PageService.GetMenu(Context));
		}

		/// <summary>
		/// Displays the a Bootstrap-styled left side menu view, including new page/settings if logged in.
		/// </summary>
		[AllowAnonymous]
		public ActionResult BootstrapNavMenu()
		{
			return Content(PageService.GetBootStrapNavMenu(Context));
		}
		
		/// <summary>
		/// Legacy action - use NavMenu().
		/// </summary>
		/// <returns></returns>
		[Obsolete]
		[AllowAnonymous]
		public ActionResult LeftMenu()
		{
			return Content(PageService.GetMenu(Context));
		}

        /// <summary>
        /// Gets an IEnumerable{SelectListItem} of project statuses, as a default
        /// SelectList doesn't add option value attributes.
        /// </summary>
        public static List<SelectListItem> ProjectStatusTypesAsSelectList
        {

            get
            {
                string[] strStatuses = new string[] { "All", "Concept", "Discovery", "Alpha", "Beta", "Live", "Decommissioned" };

                List<SelectListItem> items = new List<SelectListItem>();

                foreach (string status in strStatuses)
                {

                    SelectListItem item = new SelectListItem();
                    item.Text = status;
                    item.Value = status.ToLower();

                    items.Add(item);
                }

                return items;
            }

        }

        /// <summary>
        /// Gets an IEnumerable{SelectListItem} of project statuses, as a default
        /// SelectList doesn't add option value attributes.
        /// </summary>
        public static List<SelectListItem> LanguageTypesAsSelectList
        {
            get
            {
                string[] strLanguages = new string[] { "All", "Non-dependant", "C#", "Java", "JavaScript", "PHP", "Ruby", "Other" };

                List<SelectListItem> items = new List<SelectListItem>();

                foreach (string language in strLanguages)
                {

                    SelectListItem item = new SelectListItem();
                    item.Text = language;
                    item.Value = language.ToLower();

                    items.Add(item);
                }

                return items;
            }
        }

        /// <summary>
        /// Gets an IEnumerable{SelectListItem} of project statuses, as a default
        /// SelectList doesn't add option value attributes.
        /// </summary>
        public static List<SelectListItem> OrgsAsNewSelectList
        {
            get
            {

                ApplicationSettings appSettings = new ApplicationSettings();
                appSettings.DataStoreType = DataStoreType.Sqlite;
                appSettings.ConnectionString = "Data Source=|DataDirectory|\roadkill.sqlite;";
                appSettings.LoggingTypes = "none";
                appSettings.UseBrowserCache = false;

                LightSpeedRepository repository = new LightSpeedRepository(appSettings);

                IEnumerable<Organisation> OrgList;
                OrgList = repository.FindAllOrgs();

                List<SelectListItem> items = new List<SelectListItem>();

                foreach (Organisation org in OrgList)
                {

                    SelectListItem item = new SelectListItem();
                    item.Text = org.OrgName.ToString();
                    item.Value = org.Id.ToString();

                    items.Add(item);
                }

                items.Sort((x, y) => string.Compare(x.Text, y.Text));

                return items;
            }

        }

        public ActionResult Activity(string View = null)
        {
            IEnumerable<ActivityViewModel> model = PageService.GetActivity();
            model = model.OrderByDescending(x => x.activityDateTime);

            if (model == null)
                return Content(string.Format("The page could not be found"));

            return PartialView(View, model);
        }

        public static object DatePickerAttributes
        {
            get{             
               
              
               return new { @class = "form-control datepicker" };

            }
            
          
        }
    }
}