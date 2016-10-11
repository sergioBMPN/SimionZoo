﻿using System.Xml;
using System.Collections.Generic;
using Caliburn.Micro;

using Simion;

namespace Badger.ViewModels
{
    public class AppViewModel: PropertyChangedBase
    {
        //deferred load step
        public delegate void deferredLoadStep();

        private List<deferredLoadStep> m_deferredLoadSteps= new List<deferredLoadStep>();
        public void registerDeferredLoadStep(deferredLoadStep func) { m_deferredLoadSteps.Add(func); }
        public void doDeferredLoadSteps()
        {
            foreach (deferredLoadStep deferredStep in m_deferredLoadSteps)
                deferredStep();
            m_deferredLoadSteps.Clear();
        }

        //app properties: prerrequisites, exe files, definitions...
        private List<string> m_preFiles= new List<string>();
        private List<string> m_exeFiles = new List<string>();
        private Dictionary<string,XmlNode> m_classDefinitions = new Dictionary<string, XmlNode>();
        private Dictionary<string,List<string>> m_enumDefinitions 
            = new Dictionary<string, List<string>>();
        private XmlDocument m_configDocument = new XmlDocument();

        public XmlNode getClassDefinition(string className)
        {
            return m_classDefinitions[className];
        }

        public void addEnumeratedType(XmlNode definition)
        {
            List<string> enumeratedValues = new List<string>();
            foreach (XmlNode child in definition)
            {
                enumeratedValues.Add(child.InnerText);
            }

            m_enumDefinitions.Add(definition.Attributes[XMLConfig.nameAttribute].Value
                ,enumeratedValues);
        }
        public List<string> getEnumeratedType(string enumName)
        {
            return m_enumDefinitions[enumName];
        }

        private string m_name;
        public string name { get { return m_name; }set { m_name = name; } }
        private string m_version;

        private BindableCollection<ConfigNodeViewModel> m_children= new BindableCollection<ConfigNodeViewModel>();
        public BindableCollection<ConfigNodeViewModel> children { get { return m_children; }
            set { m_children = value; NotifyOfPropertyChange(() => children); } }

        //Auxiliary XML definition files: Worlds (states and actions)
        private XmlNode m_auxDefinitions= null;

        public void loadAuxDefinitions(string fileName)
        {
            XmlDocument doc = new XmlDocument();

            doc.Load(fileName);
            m_auxDefinitions= doc.LastChild; //we take here the last node to skip the <?xml ...> initial tag
            updateXMLDefRefs();
        }
        public List<string> getAuxDefinition(string hangingFrom)
        {
            //HARD-CODED: the list is filled with the contents of the nodes from: ../<hangingFrom>/Variable/Name
            List<string> definedValues = new List<string>();

            foreach (XmlNode child in m_auxDefinitions.ChildNodes)
            {
                if (child.Name==hangingFrom)
                {
                    foreach(XmlNode child2 in child.ChildNodes)
                    {
                        if (child2.Name=="Variable")
                        {
                            foreach (XmlNode child3 in child2.ChildNodes)
                            {
                                if (child3.Name == "Name")
                                    definedValues.Add(child3.InnerText);
                            }
                        }
                    }
                }
            }
            return definedValues;
        }

        //XMLDefRefs
        private List<deferredLoadStep> m_XMLDefRefListeners = new List<deferredLoadStep>();
        public void registerXMLDefRef(deferredLoadStep func) { m_XMLDefRefListeners.Add(func); }
        public void updateXMLDefRefs()
        {
            foreach (deferredLoadStep func in m_XMLDefRefListeners)
                func();
        }

        //This constructor builds the whole tree of ConfigNodes either
        // -with default values ("New")
        // -with a configuration file ("Load")
        public AppViewModel(string fileName,XmlDocument configFile= null)
        {
            m_configDocument.Load(fileName);

            foreach (XmlNode rootChild in m_configDocument.ChildNodes)
            {
                if (rootChild.Name == XMLConfig.appNodeTag)
                {
                    //APP node
                    m_preFiles.Clear();
                    m_exeFiles.Clear();
                    m_name = rootChild.Attributes["Name"].Value;
                    m_version = rootChild.Attributes["FileVersion"].Value;

                    foreach (XmlNode child in rootChild.ChildNodes)
                    {
                        //Only EXE, PRE, INCLUDE and BRANCH children nodes
                        if (child.Name == XMLConfig.exeNodeTag) m_exeFiles.Add(child.InnerText);
                        else if (child.Name == XMLConfig.preNodeTag) m_preFiles.Add(child.InnerText);
                        else if (child.Name == XMLConfig.includeNodeTag)
                            loadIncludedDefinitionFile(child.InnerText);
                        else 
                            m_children.Add(ConfigNodeViewModel.getInstance(this,child, m_name,configFile));
                            //here we assume definitions are before the children
                    }
                }
            }
            //deferred load step: enumerated types
            doDeferredLoadSteps();
        }

        private void loadIncludedDefinitionFile(string appDefinitionFile)
        {
            XmlDocument definitionFile = new XmlDocument();
            definitionFile.Load(appDefinitionFile);
            foreach (XmlNode child in definitionFile.ChildNodes)
            {
                if (child.Name == XMLConfig.definitionNodeTag)
                {
                    foreach (XmlNode definition in child.ChildNodes)
                    {
                        string name = definition.Attributes[XMLConfig.nameAttribute].Value;
                        if (definition.Name == XMLConfig.classDefinitionNodeTag)
                            m_classDefinitions.Add(name, definition);
                        else if (definition.Name == XMLConfig.enumDefinitionNodeTag)
                            addEnumeratedType(definition);
                    }
                }
            }
        }
    }
}
