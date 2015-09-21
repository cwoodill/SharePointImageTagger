using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SharePointImageTagger;

namespace SharePointImageTaggerUnitTests
{

    public class SharePointServiceUnitTests
    {

        [Test]
        public void testGetPicturesFromProfile()
        {
            SharePointOnlineService service = new SharePointOnlineService("https://xxx.sharepoint.com/", "https://xxx-my.sharepoint.com", "admin@xxx.onmicrosoft.com", "xxx", "https://xxx.sharepoint.com/TrainingImages/");
            service.findTrainingPhotosFromUserProfile();
        }

        [Test]
        public void testCreatePersonGroup()
        {
            SharePointOnlineService service = new SharePointOnlineService("https://xxx.sharepoint.com/", "https://xxx-my.sharepoint.com", "admin@xxx.onmicrosoft.com", "xxx", "https://xxx.sharepoint.com/TrainingImages/");
            FaceTagger tagger = new FaceTagger("subscriptionkey");
            var task = Task.Run(async () => await tagger.createFaceGroup("trainingphotos"));
            
            while (!task.IsCompleted)
            {

            }
        }

        [Test]
        public void testAddTrainingPhotosToPersonGroup()
        {
            SharePointOnlineService service = new SharePointOnlineService("https://xxx.sharepoint.com/", "https://xxx-my.sharepoint.com", "admin@xxx.onmicrosoft.com", "xxx", "https://xxx.sharepoint.com/TrainingImages/");
            Dictionary<string, PhotoPerson> photos = service.getTrainingPhotos();
            FaceTagger tagger = new FaceTagger("subscriptionkey");
            var task = Task.Run(async () => await tagger.createFaceGroup("trainingphotos"));
            while (!task.IsCompleted)
            {

            }
            task = Task.Run(async () => await tagger.addPhotosToTrainingGroup(photos, "trainingphotos"));
            while (!task.IsCompleted)
            {

            }

        }

        [Test]
        public void testGetPhotosToTagPhotos()
        {
            SharePointOnlineService service = new SharePointOnlineService("https://xxx.sharepoint.com/", "https://xxx-my.sharepoint.com", "admin@xxx.onmicrosoft.com", "xxx", "https://xxx.sharepoint.com/TrainingImages/", "https://xxx.sharepoint.com/PicturesToTag/");
            List<Photo> photos = service.getPhotosToTag();
        }

        [Test]
        public void testAddPhotosToTrainingGroupAndTagPhotos()
        {
            SharePointOnlineService service = new SharePointOnlineService("https://xxx.sharepoint.com/", "https://xxx-my.sharepoint.com", "admin@xxx.onmicrosoft.com", "xxx", "https://xxx.sharepoint.com/TrainingImages/", "https://xxx.sharepoint.com/PicturesToTag/");
            Dictionary<string, PhotoPerson> photos = service.getTrainingPhotos();
            FaceTagger tagger = new FaceTagger("subscriptionkey");

            
            var task = Task.Run(async () => await tagger.createFaceGroup("trainingphotos"));
            while (!task.IsCompleted)
            {

            }

            if (task.IsFaulted)
            {
                throw task.Exception;
            }

            task = Task.Run(async () => await tagger.addPhotosToTrainingGroup(photos, "trainingphotos"));
            while (!task.IsCompleted)
            {

            }

            if (task.IsFaulted)
            {
                throw task.Exception;
            }

            List<Photo> photosToTag = service.getPhotosToTag();
            task = Task.Run(async () => await tagger.identifyPhotosInGroup("trainingphotos", photosToTag));
            while (!task.IsCompleted)
            {

            }

            if (task.IsFaulted)
            {
                throw task.Exception;
            }

            service.updateTaggedPhotosWithMatchedPeople(photosToTag);
            
        }

        [Test]
        public void testGetTrainingPhotos()
        {
            SharePointOnlineService service = new SharePointOnlineService("https://xxx.sharepoint.com/", "https://xxx-my.sharepoint.com", "admin@xxx.onmicrosoft.com", "xxx", "https://xxx.sharepoint.com/TrainingImages/");
            Dictionary<string, PhotoPerson> photos = service.getTrainingPhotos();
        }

        [Test]
        public void testGetTextfromPhoto()
        {
            SharePointOnlineService service = new SharePointOnlineService("https://xxx.sharepoint.com/", "https://xxx-my.sharepoint.com", "admin@xxx.onmicrosoft.com", "xxx", "https://xxx.sharepoint.com/TrainingImages/", "https://xxx.sharepoint.com/ImagesWithText/");
            List<Photo> photos = service.getPhotosToTag();
            OCRTagger tagger = new OCRTagger("subscriptionkey");
            var task = Task.Run(async () => await tagger.identifyTextInPhoto(photos));
            while (!task.IsCompleted)
            {

            }
            service.updateTaggedPhotosWithText(photos);

            if (task.IsFaulted)
            {
                throw task.Exception;
            }
        }

    }
}
