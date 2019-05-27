using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication.Helpers;
using WebApplication.Models;

namespace WebApplication.Controllers
{
    public class BlobController : Controller
    {
        const string BLOBNAME = "images";
        const string TABLENAME = "ImagesTable";

        public ActionResult Index()
        {
            Helper blobManager = new Helper(BLOBNAME);
            List<string> fileList = blobManager.BlobList();
            return View(fileList);
        }

        public ActionResult Upload()
        {
            var repository = new Helper(TABLENAME);
            var existingPartitionKeys = repository.GetExistingPartitionKeys().Select(p => p.PartitionKey).Distinct().ToList();
            ViewBag.ExistingPartitionKeys = existingPartitionKeys.Count == 0 ? null : existingPartitionKeys;

            return View();
        }

        [HttpPost]
        public ActionResult Upload(HttpPostedFileBase uploadFile, ImagesTableModel model)
        {
            foreach (string file in Request.Files)
            {
                uploadFile = Request.Files[file];
            }

            //Add to blob
            Helper blobManager = new Helper(BLOBNAME);
            string fileAbsoluteUri = blobManager.UploadFile(uploadFile, model);

            //add to tableStorage
            var repository = new Helper(TABLENAME);

            repository.CreateOrUpdate(new ImageTableEntity
            {
                PartitionKey = model.Group,
                RowKey = model.Name,
                URL = fileAbsoluteUri,
                Description = model.Description
            });

            return RedirectToAction("Index", "Table");
        }

        //public ActionResult Delete(string uri)
        //{
        //    Helper blobManager = new Helper(BLOBNAME);
        //    blobManager.DeleteBlob(uri);
        //    return RedirectToAction("Index");
        //}
    }
}