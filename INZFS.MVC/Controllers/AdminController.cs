using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.ContentManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace INZFS.MVC.Controllers
{
    public class AdminController : Controller
    {

        public ActionResult ChildOne()
        {
            return View();
        }

        public ActionResult ChildTwo()
        {
            return View();
        }

    }
}
