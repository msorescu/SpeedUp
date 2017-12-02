﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34209
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SpeedUp.ProductionServiceReference {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(Namespace="http://tcs.topproducer.com/", ConfigurationName="ProductionServiceReference.MlsSoap")]
    public interface MlsSoap {
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tcs.topproducer.com/SearchMls", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        string SearchMls(string i_sXMLParameter);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tcs.topproducer.com/CreateTask", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        string CreateTask(string i_sXMLParameter);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tcs.topproducer.com/GetTaskStatus", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        int GetTaskStatus(string i_sMessageHeader);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tcs.topproducer.com/GetTaskResult", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        string GetTaskResult(string i_sMessageHeader, string objectID);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tcs.topproducer.com/GetTaskProgress", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        int GetTaskProgress(string i_sMessageHeader);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tcs.topproducer.com/GetFileContent", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        string GetFileContent(string path);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tcs.topproducer.com/GetDefList", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        string GetDefList(string i_sXMLParameter);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tcs.topproducer.com/SetTmkLogMode", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        string SetTmkLogMode(string logOn);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tcs.topproducer.com/GetTmkLog", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        string GetTmkLog();
        
        // CODEGEN: Generating message contract since the operation UpdateMlsBoards is neither RPC nor document wrapped.
        [System.ServiceModel.OperationContractAttribute(Action="http://tcs.topproducer.com/UpdateMlsBoards", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        SpeedUp.ProductionServiceReference.UpdateMlsBoardsResponse UpdateMlsBoards(SpeedUp.ProductionServiceReference.UpdateMlsBoardsRequest request);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tcs.topproducer.com/GetMetadata", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        string GetMetadata(string i_sXMLParameter);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tcs.topproducer.com/GetMLSFieldsAndCriteria", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        string GetMLSFieldsAndCriteria(string i_sXMLParameter);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tcs.topproducer.com/PublishCategorizationPackage", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        string PublishCategorizationPackage(string moduleID);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tcs.topproducer.com/GetCategorizationPackage", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        string GetCategorizationPackage(string moduleID);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tcs.topproducer.com/PublishCategorizationPackageWithDataSourceID", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        string PublishCategorizationPackageWithDataSourceID(string moduleID, string dataSourceID);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tcs.topproducer.com/GetCategorizationPackageWithDataSourceID", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        string GetCategorizationPackageWithDataSourceID(string moduleID, string dataSourceID);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tcs.topproducer.com/GetMasterCategoryList", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        string GetMasterCategoryList();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tcs.topproducer.com/PublishMasterCategoryList", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        string PublishMasterCategoryList(string xmlInfo);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tcs.topproducer.com/GetBoardIDByModuleID", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        string GetBoardIDByModuleID(string moduleID);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tcs.topproducer.com/GetCategoryName", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        string GetCategoryName(string displayName);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tcs.topproducer.com/GetCategoryNameByPropertyClass", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        string GetCategoryNameByPropertyClass(string displayName, string propertyClass);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tcs.topproducer.com/GetTCSStandardFieldMapping", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        string GetTCSStandardFieldMapping(string i_sXMLParameter);
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34234")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://tcs.topproducer.com/")]
    public partial class any : object, System.ComponentModel.INotifyPropertyChanged {
        
        private System.Xml.XmlNode[] anyField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute()]
        [System.Xml.Serialization.XmlAnyElementAttribute(Order=0)]
        public System.Xml.XmlNode[] Any {
            get {
                return this.anyField;
            }
            set {
                this.anyField = value;
                this.RaisePropertyChanged("Any");
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(IsWrapped=false)]
    public partial class UpdateMlsBoardsRequest {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="", Order=0)]
        public SpeedUp.ProductionServiceReference.any xmlRequest;
        
        public UpdateMlsBoardsRequest() {
        }
        
        public UpdateMlsBoardsRequest(SpeedUp.ProductionServiceReference.any xmlRequest) {
            this.xmlRequest = xmlRequest;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(IsWrapped=false)]
    public partial class UpdateMlsBoardsResponse {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="", Order=0)]
        public SpeedUp.ProductionServiceReference.any UpdateMlsBoardsResult;
        
        public UpdateMlsBoardsResponse() {
        }
        
        public UpdateMlsBoardsResponse(SpeedUp.ProductionServiceReference.any UpdateMlsBoardsResult) {
            this.UpdateMlsBoardsResult = UpdateMlsBoardsResult;
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface MlsSoapChannel : SpeedUp.ProductionServiceReference.MlsSoap, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class MlsSoapClient : System.ServiceModel.ClientBase<SpeedUp.ProductionServiceReference.MlsSoap>, SpeedUp.ProductionServiceReference.MlsSoap {
        
        public MlsSoapClient() {
        }
        
        public MlsSoapClient(string endpointConfigurationName) : 
                base(endpointConfigurationName) {
        }
        
        public MlsSoapClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public MlsSoapClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public MlsSoapClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        public string SearchMls(string i_sXMLParameter) {
            return base.Channel.SearchMls(i_sXMLParameter);
        }
        
        public string CreateTask(string i_sXMLParameter) {
            return base.Channel.CreateTask(i_sXMLParameter);
        }
        
        public int GetTaskStatus(string i_sMessageHeader) {
            return base.Channel.GetTaskStatus(i_sMessageHeader);
        }
        
        public string GetTaskResult(string i_sMessageHeader, string objectID) {
            return base.Channel.GetTaskResult(i_sMessageHeader, objectID);
        }
        
        public int GetTaskProgress(string i_sMessageHeader) {
            return base.Channel.GetTaskProgress(i_sMessageHeader);
        }
        
        public string GetFileContent(string path) {
            return base.Channel.GetFileContent(path);
        }
        
        public string GetDefList(string i_sXMLParameter) {
            return base.Channel.GetDefList(i_sXMLParameter);
        }
        
        public string SetTmkLogMode(string logOn) {
            return base.Channel.SetTmkLogMode(logOn);
        }
        
        public string GetTmkLog() {
            return base.Channel.GetTmkLog();
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        SpeedUp.ProductionServiceReference.UpdateMlsBoardsResponse SpeedUp.ProductionServiceReference.MlsSoap.UpdateMlsBoards(SpeedUp.ProductionServiceReference.UpdateMlsBoardsRequest request) {
            return base.Channel.UpdateMlsBoards(request);
        }
        
        public SpeedUp.ProductionServiceReference.any UpdateMlsBoards(SpeedUp.ProductionServiceReference.any xmlRequest) {
            SpeedUp.ProductionServiceReference.UpdateMlsBoardsRequest inValue = new SpeedUp.ProductionServiceReference.UpdateMlsBoardsRequest();
            inValue.xmlRequest = xmlRequest;
            SpeedUp.ProductionServiceReference.UpdateMlsBoardsResponse retVal = ((SpeedUp.ProductionServiceReference.MlsSoap)(this)).UpdateMlsBoards(inValue);
            return retVal.UpdateMlsBoardsResult;
        }
        
        public string GetMetadata(string i_sXMLParameter) {
            return base.Channel.GetMetadata(i_sXMLParameter);
        }
        
        public string GetMLSFieldsAndCriteria(string i_sXMLParameter) {
            return base.Channel.GetMLSFieldsAndCriteria(i_sXMLParameter);
        }
        
        public string PublishCategorizationPackage(string moduleID) {
            return base.Channel.PublishCategorizationPackage(moduleID);
        }
        
        public string GetCategorizationPackage(string moduleID) {
            return base.Channel.GetCategorizationPackage(moduleID);
        }
        
        public string PublishCategorizationPackageWithDataSourceID(string moduleID, string dataSourceID) {
            return base.Channel.PublishCategorizationPackageWithDataSourceID(moduleID, dataSourceID);
        }
        
        public string GetCategorizationPackageWithDataSourceID(string moduleID, string dataSourceID) {
            return base.Channel.GetCategorizationPackageWithDataSourceID(moduleID, dataSourceID);
        }
        
        public string GetMasterCategoryList() {
            return base.Channel.GetMasterCategoryList();
        }
        
        public string PublishMasterCategoryList(string xmlInfo) {
            return base.Channel.PublishMasterCategoryList(xmlInfo);
        }
        
        public string GetBoardIDByModuleID(string moduleID) {
            return base.Channel.GetBoardIDByModuleID(moduleID);
        }
        
        public string GetCategoryName(string displayName) {
            return base.Channel.GetCategoryName(displayName);
        }
        
        public string GetCategoryNameByPropertyClass(string displayName, string propertyClass) {
            return base.Channel.GetCategoryNameByPropertyClass(displayName, propertyClass);
        }
        
        public string GetTCSStandardFieldMapping(string i_sXMLParameter) {
            return base.Channel.GetTCSStandardFieldMapping(i_sXMLParameter);
        }
    }
}