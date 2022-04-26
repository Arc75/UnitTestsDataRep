using EleWise.ELMA.ConfigurationModel;
using EleWise.ELMA.Model.Common;
using EleWise.ELMA.Model.Entities.ProcessContext;
using EleWise.ELMA.Model.Scripts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestMocksGenerator;
using TestMocksGenerator.Models.Const;

namespace T_DownloadAllDocs_Tests
{
    [TestClass]
    public class UnderwritingClientTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            var context = MockHelper.GetWorkflow<P_UnderwritingClient>(EntityTypes.P_UnderwritingClient, "116252", new List<string> { "Sale", "LoanSubject" });

            var extracts = MockHelper.GetEntitiesQuery<BGF_USRNExtract>(EntityTypes.BGF_USRNExtract, $"Sale <> null AND Sale = {context.Sale.Id} And CadastralOrConditionalNumber = '{context.Sale.LoanSubject.CadastralOrConditionalNumber}'", true);

            var sysParams = MockHelper.GetSysParams("RequestExtractUSRN", "ErrorUSRNStatus");

            var mock = new Mock<P_UnderwritingClient_Scripts> { CallBase = true };
            mock.Setup(x => x.GetSysParams()).Returns(sysParams);
            mock.Setup(x => x.FindUSRNExtract()).Returns(extracts);

            var result = mock.Object.CheckUSRN(context);
        }
    }
}
