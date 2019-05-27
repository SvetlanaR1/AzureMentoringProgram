using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication.Helpers;
using WebApplication.Models;

namespace WebApplication.Controllers
{
    public class TableController : Controller
    {
        const string TABLENAME = "ImagesTable";
        const string BLOBNAME = "images";
        public ActionResult Index()
        {
            var repository = new Helper(TABLENAME);
            var entities = repository.GetAllRecords();
            var models = entities.Select(x => new ImagesTableModel
            {
                Group = x.PartitionKey,
                Name = x.RowKey,
                URL = x.URL,
                Description = x.Description
            });
            return View(models);
        }

        public ActionResult ConfirmDelete(string name, string group)
        {
            var repository = new Helper(TABLENAME);
            var item = repository.Get(group, name);
            repository.Delete(item);

            Helper blobManager = new Helper(BLOBNAME);
            blobManager.DeleteBlob(item.URL);

            return RedirectToAction("Index");
        }

        public ActionResult Edit(string name, string group)
        {
            var repository = new Helper(TABLENAME);
            var item = repository.Get(group, name);

            Helper blobManager = new Helper(BLOBNAME);
            List<string> fileList = blobManager.BlobList();
            ViewBag.ExistingUrls = fileList;
            ViewBag.ExistingPartitionKeys = repository.GetExistingPartitionKeys().Select(p => p.PartitionKey).Distinct().ToList();

            return View(new ImagesTableModel
            {
                Group = item.PartitionKey,
                Name = item.RowKey,
                URL = item.URL,
                Description = item.Description
            });
        }

        [HttpPost]
        public ActionResult Edit(ImagesTableModel model)
        {
            var repository = new Helper(TABLENAME);
            var item = repository.Get(model.Group, model.Name);
            item.Name = model.Name;
            item.URL = model.URL;
            item.Description = model.Description;

            repository.CreateOrUpdate(item);

            return RedirectToAction("Index");
        }
    }
}