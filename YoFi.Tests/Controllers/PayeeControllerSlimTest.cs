using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using YoFi.AspNet.Controllers;
using YoFi.Core.Models;
using YoFi.Core.Repositories;
using YoFi.Tests.Helpers;

namespace YoFi.Tests.Controllers.Slim
{
    [TestClass]
    public class PayeeControllerSlimTest: BaseControllerSlimTest<Payee>
    {
        //Enable when needed
        //private PayeesController payeeController => base.controller as PayeesController;

        [TestInitialize]
        public void SetUp()
        {
            repository = new MockPayeeRepository();
            controller = new PayeesController(repository as IPayeeRepository);
        }
    }
}
