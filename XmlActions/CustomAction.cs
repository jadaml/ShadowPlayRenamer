using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Deployment.WindowsInstaller;

namespace XmlActions
{
    public class CustomActions
    {
        [CustomAction]
        public static ActionResult ModifyXmlFile(Session session)
        {
            session.Log("Changing Configuration.");

            try
            {
                session.Log("ModifyConfigArgs property: {0}", session["ModifyConfigArgs"]);
            }
            catch (Exception) { }

            session.Log("Custom action data:\n{0}", string.Join("\n", session.CustomActionData.Select(e => $"{e.Key}={e.Value}")));

            CustomActionData data = new CustomActionData(session.CustomActionData["ModifyConfigArgs"]);

            string configFile  = data["ConfigFile"];
            string configKey   = data["Key"];
            string configValue = data["Value"];

            session.Log("Configuration file: {0}, Configuration key: {1}, New value: {2}", configFile, configKey, configValue);

            XmlDocument doc = new XmlDocument();

            try
            {
                using (FileStream config = File.OpenRead(configFile)) doc.Load(config);

                XmlNodeList list = doc.SelectNodes($"/configuration/appSettings/add[@key=\"{configKey}\"]");

                var query = from XmlNode node in list
                            where !(node.Attributes[configKey] is null)
                            select node;

                foreach (XmlNode item in query)
                {
                    item.Attributes[configKey].Value = configValue;
                }

                using (FileStream config = File.OpenWrite(configFile)) doc.Save(config);
            }
            catch (Exception ex)
            {
                session.Log("Failed to change the file:\n{0}", ex);

                return ActionResult.Failure;
            }

            return ActionResult.Success;
        }
    }
}
