//######################################################################
//# FILENAME: ApplicationInstanceChecker
//#
//# DESCRIPTION:
//# 
//#
//# AUTHOR:		Mohammad Saiful Alam
//# POSITION:	Senior General Manager
//# E-MAIL:		saiful.alam@ bjitgroup.com
//# CREATE DATE: ...
//#
//# Copyright (c): Free to use
//######################################################################
using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;

namespace Downloader
{
    class FileDownloader: AbsDownloader
    {
        public FileDownloader(System.Windows.Forms.Label progress) : base(progress)
        {
         
        }

        //
        public async System.Threading.Tasks.Task DownloadAsync(string remoteUri, IDownloadObserver observer)
        {
            this.url = remoteUri;
            // Set the Notiifcation Observer..
            this.DownloadObserver = observer;   
            //
            String fileName = Path.GetFileName(remoteUri);
            string filePath = Directory.GetCurrentDirectory() + "\\" + fileName;
            this.downloadLocation = filePath;

            mHttpClientWithProgress = new HttpClientWithProgress(remoteUri, filePath, this);
            {
                this.downloadStatus = "Continue";
                //
                mHttpClientWithProgress.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) => {
                    //Console.WriteLine($"{progressPercentage}% ({totalBytesDownloaded}/{totalFileSize})");
                    this.uodateStatus($"{progressPercentage}% ({totalBytesDownloaded}/{totalFileSize})");
                    if(progressPercentage == 100)
                    {
                        this.onComplete(this, new AsyncCompletedEventArgs(null, false, null));
                    }
                };



                await mHttpClientWithProgress.StartDownload();
            }
        }
    }
}
