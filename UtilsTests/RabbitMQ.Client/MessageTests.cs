using Microsoft.VisualStudio.TestTools.UnitTesting;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Tests
{
    [TestClass()]
    public class MessageTests
    {
        [TestMethod()]
        public void CastTest()
        {
            IMessage msg = new Message("{\"Name\":\"base\",\"Value\":\"42\"}");
            IMessage<NameValue> msg1 = msg.Cast<NameValue>();
            Assert.IsNotNull(msg1.Value);
        }

        public class NameValue
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }
    }
}