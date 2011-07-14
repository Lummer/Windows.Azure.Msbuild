﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Windows.Azure.Msbuild.AzureTools;
using System.IO;

namespace Windows.Azure.Msbuild
{
    public class CopyToAzureStorage : Task
    {
        [Required]
        public string ContainerName { get; set; }

        [Required]
        public string Endpoint { get; set; }

        [Required]
        public ITaskItem[] SourceFiles { get; set; }

        [Required]
        public ITaskItem[] DestinationFiles { get; set; }

        [Required]
        public string StorageAccountKey { get; set; }

        [Required]
        public string StorageAccountName { get; set; }

        public override bool Execute()
        {
            var msg = "Creating cloud storage client with Endpoint: {0}, StorageAccountKey: {1}, StorageAccountName: {2}";
            logger.LogMessage(msg, Endpoint, StorageAccountKey, StorageAccountName);

            var endpoint = new Uri(Endpoint);
            var client = blobClientWrapper.Create(endpoint, StorageAccountName, StorageAccountKey);
            var container = client.GetContainerReference(ContainerName);
            if (container.CreateIfNotExists()) {
                logger.LogMessage("Created container: '{0}'", ContainerName);
            }

            for (int i = 0; i < SourceFiles.Length; i++)
            {
            	var pathToSource = SourceFiles[i].ItemSpec;
                var pathToDest = (DestinationFiles != null) ? DestinationFiles[i].ItemSpec : Path.GetFileName(pathToSource);
                var contentType = SourceFiles[i].GetMetadata("ContentType");

                if (String.IsNullOrEmpty(contentType))
                {
                    var blob = container.GetBlobReference(pathToDest);
                    if(blob.DeleteIfExists()) {
                        logger.LogMessage("Deleted file: '{0}', from cloud storage", pathToDest);
                    }

                    using (Stream stream = fileManager.GetFile(pathToSource)) {
                        blob.UploadFromStream(stream);
                    }
                    
                }
            }
            

            return true;
        }

        [CoverageExclude(Reason.Humble)]
        public CopyToAzureStorage() : this(new CloudBlobClientFactory(), null, new FileManager())
        {
            
        }

        [CoverageExclude(Reason.Test)]
        public CopyToAzureStorage(ICloudBlobClientWrapper blobClientWrapper, ITaskLogger taskLogger, IFileManager fileManager)
        {
            if (taskLogger == null)
                taskLogger = new MyTaskLogger(this);

            this.logger = taskLogger;
            this.fileManager = fileManager;
            this.blobClientWrapper = blobClientWrapper;            
        }

        private readonly ICloudBlobClientWrapper blobClientWrapper;
        private readonly IFileManager fileManager;
        private readonly ITaskLogger logger;
    }
}