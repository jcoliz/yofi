using Common.AspNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using YoFi.Core.Models;
using YoFi.Core.Repositories;
using YoFi.Tests.Helpers;

namespace YoFi.Tests.Controllers.Slim
{
    [TestClass]
    public class BaseControllerSlimTest<T> where T: class, IModelItem, new()
    {
        protected IController<T> controller;
        protected IMockRepository<T> repository;
    }
}
