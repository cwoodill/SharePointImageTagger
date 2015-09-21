using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Web;
using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;
using System.IO;

namespace SharePointImageTagger
{
    public class OCRTagger
    {
        public string SubscriptionKey { get; set; }

        public bool DetectOrientation { get; set; }

        public string Language { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="SubscriptionKey">Subscript key provided by the Microsoft API site.</param>
        public OCRTagger(string SubscriptionKey)
        {
            this.SubscriptionKey = SubscriptionKey;
            this.Language = "unk";
            this.DetectOrientation = true;
        }

        /// <summary>
        /// Identifies text found in a list of photos.  Text found is added as a property back into each photo.
        /// </summary>
        /// <param name="Photos">Provided list of photos.</param>
        /// <returns></returns>
        public async Task identifyTextInPhoto(List<Photo> Photos)
        {
            try
            {
                foreach (Photo photo in Photos)
                {
                    VisionServiceClient client = new VisionServiceClient(SubscriptionKey);
                    Stream stream = new MemoryStream(photo.Image);
                    OcrResults result = await client.RecognizeTextAsync(stream, Language, DetectOrientation);
                    photo.LanguageDetectedInPhoto = result.Language;
                    foreach (Region region in result.Regions)
                    {
                        for (int i=0; i< region.Lines.Length; i++)
                        {
                            Line line = region.Lines[i];
                            string lineText = "";
                            for (int j= 0; j < line.Words.Length; j++)
                            {
                                lineText += line.Words[j].Text;
                                if (j < line.Words.Length -1)
                                {
                                    lineText += " ";
                                }
                            }
                            photo.TextInPhoto.Add(lineText);
                        }
                    }

                }
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }

}