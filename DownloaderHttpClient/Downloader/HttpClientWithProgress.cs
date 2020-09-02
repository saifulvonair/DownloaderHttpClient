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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Downloader
{
    public class HttpClientWithProgress : IDisposable
    {
        private readonly string _downloadUrl;
        private readonly string _destinationFilePath;
        CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private int _maxTry = 50;
        AbsDownloader _absDownloader;

        //private HttpClient _httpClient;

        public delegate void ProgressChangedHandler(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage);

        public event ProgressChangedHandler ProgressChanged;

        public HttpClientWithProgress(string downloadUrl, string destinationFilePath, AbsDownloader absDownloader)
        {
            _downloadUrl = downloadUrl;
            _destinationFilePath = destinationFilePath;
            _absDownloader = absDownloader;
        }

        public async Task StartDownload()
        {
            HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromDays(1) };

            //CancellationTokenSource tokenSource = new CancellationTokenSource();
            // Zip file download needs this settings...
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                                            | SecurityProtocolType.Tls11
                                            | SecurityProtocolType.Tls12
                                            | SecurityProtocolType.Ssl3;

           _httpClient.DefaultRequestHeaders.ExpectContinue = false;
            HttpResponseMessage response = null;
            try
            {
                response = await _httpClient.GetAsync(
             _downloadUrl,
             HttpCompletionOption.ResponseHeadersRead); // Important! ResponseHeadersRead.
                var totalBytes = response.Content.Headers.ContentLength;
                if (totalBytes == null && _maxTry >= 0)
                {
                    _maxTry--;
                    _httpClient.Dispose();
                    _httpClient = null;
                    //TODO
                    _absDownloader.uodateStatus("Trying: Out of 10 to: " + _maxTry);
                    //
                    await StartDownload();
                }
                else
                {
                    _maxTry = 10;
                }
            }
            catch (Exception)
            {
                if (_maxTry >= 0)
                {
                    await StartDownload();
                }
            }
            //
            await DownloadFileFromHttpResponseMessage(response);
        }

        private async Task DownloadFileFromHttpResponseMessage(HttpResponseMessage response)
        {
            if(response != null)
            {
                response.EnsureSuccessStatusCode();
                var totalBytes = response.Content.Headers.ContentLength;
                using (var contentStream = await response.Content.ReadAsStreamAsync())
                    await ProcessContentStream(totalBytes, contentStream);
            }
            
        }

        private async Task ProcessContentStream(long? totalDownloadSize, Stream contentStream)
        {
            var totalBytesRead = 0L;
            var readCount = 0L;
            var buffer = new byte[8192];
            var isMoreToRead = true;

            using (var fileStream = new FileStream(_destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
            {
                do
                {
                    if(totalDownloadSize == null)
                    {
                        totalDownloadSize = contentStream.Length;
                    }

                    var bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        isMoreToRead = false;
                        if(totalDownloadSize == 0 || totalDownloadSize == null)
                        {
                            totalDownloadSize = totalBytesRead;
                        }
                        TriggerProgressChanged(totalDownloadSize, totalBytesRead);
                        continue;
                    }

                    await fileStream.WriteAsync(buffer, 0, bytesRead);

                    totalBytesRead += bytesRead;
                    readCount += 1;
                    long prog = readCount % 100;
                    if (prog >= 0)
                        TriggerProgressChanged(totalDownloadSize, totalBytesRead);
                  
                    if (_tokenSource.IsCancellationRequested)
                    {
                        return;
                    }
                }
                while (isMoreToRead);
            }
        }

        private void TriggerProgressChanged(long? totalDownloadSize, long totalBytesRead)
        {
            if (ProgressChanged == null)
                return;

            double? progressPercentage = null;
            if (totalDownloadSize.HasValue)
                progressPercentage = Math.Round((double)totalBytesRead / totalDownloadSize.Value * 100, 2);

            ProgressChanged(totalDownloadSize, totalBytesRead, progressPercentage);
        }

        public void Dispose()
        {
            //_httpClient?.Dispose();
        }

        public void cancel()
        {
            _tokenSource.Cancel();
            // TODO...Rollback saved data..
        }
    }
}
