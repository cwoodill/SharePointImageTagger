using System;
using System.Collections.Generic;
using System.Security;

using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Search.Query;
using Microsoft.SharePoint;
using SP = Microsoft.SharePoint.Client;

using SharePointImageTagger.Exceptions;
using System.IO;

namespace SharePointImageTagger
{
    public class SharePointOnlineService
    {
        /// <summary>
        /// Hard coded GUID representing the web location in Office 365 for user profiles.
        /// </summary>
        string peopleSourceID = "B09A7990-05EA-4AF9-81EF-EDFAB16C4E31";

        /// <summary>
        /// Based URL for your SharePoint repository, e.g. https://xxx.sharepoint.com/
        /// </summary>
        public string SharePointURL { get; set; }

        /// <summary>
        /// URL to the tenant's repository for profile pictures.  Usually this is https://xxx.my-sharepoint.com/
        /// </summary>
        public string ProfilePicturesURL { get; set; }

        /// <summary>
        /// Username to access content
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Password of user to access content
        /// </summary>
        public string Password { get; set; }
        /// <summary>
        /// Request time out
        /// </summary>
        public int RequestTimeout { get; set; }

        /// <summary>
        /// URL to the list of training photos 
        /// </summary>
        public string TrainingListURL { get; set; }

        /// <summary>
        /// URL to the list of Photos to tag
        /// </summary>
        public string PhotosToTagURL { get; set; }
        
        // list of column names.  These are used for pulling content from lists.
        public string TrainingPersonIdColumn { get; set; }
        public string TrainingFileColumn { get; set; }
        public string TrainingIdColumn { get; set; }

        public string PhotoIdColumn { get; set;  }

        public string PhotoFileColumn { get; set; }

        public string PhotoNumberOfFacesColumn { get; set; }
        public string PhotoNumberOfMatchedFacesColumn { get; set; }
        public string PhotoNumberOfUnMachedFacesColumn { get; set; }
        public string PhotoMatchedPeopleColumn { get; set; }

        public string PhotoTextColumn { get; set; }


        public SharePointOnlineService(string SharePointURL, string ProfilePicturesURL, string Username, string Password, string TrainingListURL, string PhotosToTagURL = null)
        {
            this.SharePointURL = SharePointURL;
            this.ProfilePicturesURL = ProfilePicturesURL;
            this.Username = Username;
            this.Password = Password;
            this.RequestTimeout = 60000000;
            this.TrainingListURL = TrainingListURL;
            this.PhotosToTagURL = PhotosToTagURL;

            // these are the names of the columns in my custom lists but they could be changed to any valid columns with the right type.
            // use image libraries as the document repository for photos.

            this.TrainingPersonIdColumn = "FullName";
            this.TrainingFileColumn = "FileRef";
            this.TrainingIdColumn = "Id";

            this.PhotoFileColumn = "FileRef";
            this.PhotoIdColumn = "Id";
            this.PhotoNumberOfFacesColumn = "Number_x0020_of_x0020_Faces";
            this.PhotoNumberOfUnMachedFacesColumn = "Number_x0020_of_x0020_Unmatched_x0020_Faces";
            this.PhotoMatchedPeopleColumn = "Matched_x0020_Person";
            this.PhotoTextColumn = "TextFromImage";

        }

        /// <summary>
        /// Pull training photos from a SharePoint list and add them as a dictionary to be used for training purposess.
        /// </summary>
        /// <returns>Dictionary organized by person key of each person with their photos attached.</returns>
        public Dictionary<string, PhotoPerson> getTrainingPhotos()
        {
            Dictionary<string, PhotoPerson> trainingPhotos = new Dictionary<string, PhotoPerson>();

            using (ClientContext context = Login(SharePointURL))
            {
                try
                {
                    var list = context.Web.GetList(TrainingListURL);
                    var query = CamlQuery.CreateAllItemsQuery();
                    
                    var result = list.GetItems(query);
                    ListItemCollection items = list.GetItems(query);
                    context.Load(items, includes => includes.Include(
                        i => i[TrainingPersonIdColumn],
                        i => i[TrainingFileColumn],
                        i => i[TrainingIdColumn]));
                        
                        

                    //now you get the data
                    context.ExecuteQuery();

                    //here you have list items, but not their content (files). To download file
                    //you'll have to do something like this:

                    foreach (ListItem item in items)
                    {
                        PhotoPerson person = null;
                        if (item[TrainingPersonIdColumn] != null)
                        {
                            string fullName = (string)item[TrainingPersonIdColumn];
                            // look for existing person
                            if (trainingPhotos.ContainsKey(fullName))
                            {
                                person = trainingPhotos[fullName];
                            }
                            else
                            {
                                person = new PhotoPerson();
                                person.Name = fullName;
                                person.ID = item.Id;
                            }

                            //get the URL of the file you want:
                            var fileRef = item[TrainingFileColumn];

                            //get the file contents:
                            FileInformation fileInfo = Microsoft.SharePoint.Client.File.OpenBinaryDirect(context, fileRef.ToString());
                            
                            using (var memory = new MemoryStream())
                            {
                                byte[] buffer = new byte[1024 * 64];
                                int nread = 0;
                                while ((nread = fileInfo.Stream.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    memory.Write(buffer, 0, nread);
                                }
                                memory.Seek(0, SeekOrigin.Begin);
                                Photo photo = new Photo();
                                photo.ID = item.Id.ToString();
                                photo.Image = memory.ToArray();
                                person.Photos.Add(photo);
                            }

                            trainingPhotos.Add(fullName, person);

                        }
                        
                    }



                }
                catch (Exception e)
                {
                    throw;
                }
            }
            return trainingPhotos;

        }

        /// <summary>
        /// Used for OCR tagger - pulls a generic list of photos with IDs.
        /// </summary>
        /// <returns>List of photos</returns>
        public List<Photo> getPhotosToTag()
        {
            List<Photo> photos = new List<Photo>();

            using (ClientContext context = Login(SharePointURL))
            {
                try
                {
                    var list = context.Web.GetList(PhotosToTagURL);
                    var query = CamlQuery.CreateAllItemsQuery();

                    var result = list.GetItems(query);
                    ListItemCollection items = list.GetItems(query);
                    context.Load(items, includes => includes.Include(
                        i => i[PhotoFileColumn],
                        i => i[PhotoIdColumn]));

                    //now you get the data
                    context.ExecuteQuery();


                    //here you have list items, but not their content (files). To download file
                    //you'll have to do something like this:

                    foreach (ListItem item in items)
                    {
                        Photo photo = new Photo();
                        
                        //get the URL of the file you want:
                        var fileRef = item[PhotoFileColumn];

                        //get the file contents:
                        FileInformation fileInfo = Microsoft.SharePoint.Client.File.OpenBinaryDirect(context, fileRef.ToString());

                        using (var memory = new MemoryStream())
                        {
                            byte[] buffer = new byte[1024 * 64];
                            int nread = 0;
                            while ((nread = fileInfo.Stream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                memory.Write(buffer, 0, nread);
                            }
                            memory.Seek(0, SeekOrigin.Begin);

                            photo.ID = item.Id.ToString();
                            photo.Image = memory.ToArray();
                            photos.Add(photo);
                        }

                    }
                    
                }
                catch (Exception e)
                {
                    throw;
                }
            }


            return photos;
        }

        /// <summary>
        /// Updates the list with matched people from the Microsoft Face API
        /// </summary>
        /// <param name="Photos"></param>
        public void updateTaggedPhotosWithMatchedPeople(List<Photo> Photos)
        {
            using (ClientContext context = Login(SharePointURL))
            {
                try
                {
                    foreach (Photo photo in Photos)
                    {
                        SP.List list = context.Web.GetList(PhotosToTagURL);
                        ListItem item = list.GetItemById(photo.ID);
                        item[PhotoNumberOfFacesColumn] = photo.NumberOfMatchedFaces;
                        item[PhotoNumberOfUnMachedFacesColumn] = photo.NumberOfUnmatchedFaces;

                        FieldLookupValue[] matchedPeople = new FieldLookupValue[photo.PeopleInPhoto.Count];
                        for (int i=0; i< photo.PeopleInPhoto.Count; i++)
                        {
                            FieldLookupValue value = new FieldLookupValue();
                            value.LookupId = photo.PeopleInPhoto[i].ID;
                            matchedPeople[i] = value;
                        }
                        item[PhotoMatchedPeopleColumn] = matchedPeople;
                        item.Update();
                        context.ExecuteQuery();
                    }
                }
                catch (Exception e)
                {
                    throw;
                }
            }

        }

        /// <summary>
        /// Updates photos from the OCR Tagging with found text.
        /// </summary>
        /// <param name="Photos"></param>

        public void updateTaggedPhotosWithText(List<Photo> Photos)
        {
            using (ClientContext context = Login(SharePointURL))
            {
                try
                {
                    foreach (Photo photo in Photos)
                    {
                        SP.List list = context.Web.GetList(PhotosToTagURL);
                        ListItem item = list.GetItemById(photo.ID);
                        
                        string textInPhoto = "";

                        string[] lines = photo.TextInPhoto.ToArray();

                        for (int i = 0; i < lines.Length; i++)
                        {
                            textInPhoto += lines[i];
                            if (i < lines.Length - 1)
                                textInPhoto += "\n";
                        }
                        item[PhotoTextColumn] = textInPhoto;
                        item.Update();
                        context.ExecuteQuery();
                    }
                }
                catch (Exception e)
                {
                    throw;
                }
            }

        }

        /// <summary>
        /// Find the default list of tenant user profile photo thumbnails so we can use them as a source for training.
        /// </summary>

        public void findTrainingPhotosFromUserProfile()
        {
            using (ClientContext context = Login(SharePointURL))
            {
                try
                {
                    KeywordQuery keywordQuery = new KeywordQuery(context);
                    keywordQuery.QueryText = "*";
                    keywordQuery.SourceId = Guid.Parse(peopleSourceID);
                    keywordQuery.SelectProperties.Add("PictureURL");
                    keywordQuery.SelectProperties.Add("PreferredName");
                    keywordQuery.SelectProperties.Add("AccountName");


                    SearchExecutor searchExecutor = new SearchExecutor(context);
                    ClientResult<ResultTableCollection> results = searchExecutor.ExecuteQuery(keywordQuery);
                    context.ExecuteQuery();
                    foreach (var resultRow in results.Value[0].ResultRows)
                    {
                        if (resultRow["PictureURL"] != null)
                        {
                            string name = (string) resultRow["PreferredName"];
                            string imageURL = (string) resultRow["PictureURL"];
                            string user = (string)resultRow["AccountName"];
                            imageURL = imageURL.Replace("MThumb", "LThumb");

                            addProfilePictureToTrainingList(name, name, imageURL, "", "", user);
                        }
                        
                    }
                }
                catch (Exception e)
                {
                    throw;
                }
            }

        }

        /// <summary>
        /// Copy a profile picture from user profile thumbnail to a training list.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="title"></param>
        /// <param name="imageURL"></param>
        /// <param name="description"></param>
        /// <param name="keywords"></param>
        /// <param name="user"></param>
        private void addProfilePictureToTrainingList(string name, string title, string imageURL, string description, string keywords, string user)
        {

            byte[] documentStream = null;

            if (TrainingListURL != null)
            {
                try
                {
                    using (ClientContext context = Login(ProfilePicturesURL))
                    {
                        if (context.HasPendingRequest)
                            context.ExecuteQuery();

                        Uri testuri = new Uri(imageURL);

                        //get the file contents:
                        FileInformation fileInfo = Microsoft.SharePoint.Client.File.OpenBinaryDirect(context, testuri.AbsolutePath);

                        using (var memory = new MemoryStream())
                        {
                            byte[] buffer = new byte[1024 * 64];
                            int nread = 0;
                            while ((nread = fileInfo.Stream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                memory.Write(buffer, 0, nread);
                            }
                            memory.Seek(0, SeekOrigin.Begin);
                            documentStream = memory.ToArray();
                        }
                    }

                    using (ClientContext context = Login(SharePointURL))
                    {
                        // Assume that the web has a list named "Announcements". 
                        List trainingList = context.Web.GetList(TrainingListURL);


                        var fileCreationInformation = new FileCreationInformation();
                        //Assign to content byte[] i.e. documentStream

                        fileCreationInformation.Content = documentStream;
                        //Allow owerwrite of document

                        fileCreationInformation.Overwrite = true;
                        //Upload URL

                        fileCreationInformation.Url = TrainingListURL + name + ".jpg";
                        Microsoft.SharePoint.Client.File uploadFile = trainingList.RootFolder.Files.Add(
                            fileCreationInformation);

                        //Update the metadata for a field having name "DocType"
                        uploadFile.ListItemAllFields["Title"] = title;
                        uploadFile.ListItemAllFields["Description"] = description;
                        uploadFile.ListItemAllFields["Keywords"] = keywords;
                        uploadFile.ListItemAllFields["FullName"] = name;
                        User spUser = context.Web.EnsureUser(user);
                        uploadFile.ListItemAllFields["Employee"] = spUser;
                        
                        uploadFile.ListItemAllFields.Update();
                        context.ExecuteQuery();

                    }
                }
                catch (Exception e)
                {
                    throw;
                }
            }

        }

        /// <summary>
        /// Login into SharePoint / Office 365
        /// </summary>
        /// <param name="URL"></param>
        /// <returns></returns>
                        
        protected ClientContext Login(string URL)
        {
            try
            {
                using (ClientContext clientContext = new ClientContext(URL))
                {
                    SecureString password = new SecureString();
                    foreach (char c in Password.ToCharArray()) password.AppendChar(c);
                    clientContext.Credentials = new SharePointOnlineCredentials(Username, password);

                    if (RequestTimeout > 0)
                        clientContext.RequestTimeout = RequestTimeout;

                    Web web = clientContext.Web;
                    clientContext.Load(web);
                    clientContext.ExecuteQuery();
                    return clientContext;
                }
            }
            catch (Exception e)
            {
                throw new InvalidLoginException("Invalid login trying to connect to " + URL + ".  Username attempted was " + Username + ".", e.InnerException);
            }
        }
   }
}
