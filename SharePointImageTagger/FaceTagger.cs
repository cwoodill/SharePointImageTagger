using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ProjectOxford.Face.Contract;
using Microsoft.ProjectOxford.Face;
using System.Collections.ObjectModel;
using System.IO;
using SharePointImageTagger.Exceptions;

namespace SharePointImageTagger
{
    /// <summary>
    /// Class for interacting with Microsoft Face Recognition API through the Project Oxford supplied Face API Client library.
    /// </summary>
    public class FaceTagger
    {

        /// <summary>
        /// Subscription Key guid from the Microsoft API portal
        /// </summary>
        public string SubscriptionKey { get; set; }

        /// <summary>
        /// Training photos to send to Microsoft Face Recongition API
        /// </summary>
        public Dictionary<Guid, PhotoPerson> TrainingPhotos { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="SubscriptionKey">Subscription key</param>
        public FaceTagger(string SubscriptionKey)
        {
            this.SubscriptionKey = SubscriptionKey;
            TrainingPhotos = new Dictionary<Guid, PhotoPerson>();
        }
        
        /// <summary>
        /// Creates a training group.  
        /// </summary>
        /// <param name="PersonGroupID">Name of the person group.</param>
        /// <returns></returns>
        public async Task createFaceGroup(string PersonGroupID)
        {
            bool groupExists = false;
            IFaceServiceClient faceClient = new FaceServiceClient(SubscriptionKey);
            
            // Test whether the group already exists
            try
            {
                await faceClient.GetPersonGroupAsync(PersonGroupID);
                groupExists = true;
            }
            catch (ClientException ex)
            {
                if (ex.Error.Code != "PersonGroupNotFound")
                {
                    throw;
                }
                else
                {

                }
            }

            // check to see if group exists and if so delete the group.
            if (groupExists)
            {
                await faceClient.DeletePersonGroupAsync(PersonGroupID);
            }

            try
            {
                await faceClient.CreatePersonGroupAsync(PersonGroupID, PersonGroupID);
            }
            catch (ClientException ex)
            {
                throw;
            }



        }

        /// <summary>
        /// Identify a list of photos based on an existing training group.  
        /// </summary>
        /// <param name="PersonGroupID">Name of the training group</param>
        /// <param name="Photos">List of photos to be tagged</param>
        /// <returns></returns>
        public async Task identifyPhotosInGroup(string PersonGroupID, List<Photo> Photos)
        {
            IFaceServiceClient faceClient = new FaceServiceClient(SubscriptionKey);
                        
            try
            {
                foreach (Photo photo in Photos)
                {
                    photo.NumberOfMatchedFaces = 0;
                    photo.NumberOfUnmatchedFaces = 0;
                    photo.PeopleInPhoto.Clear();

                    // convert image bytes into a stream
                    Stream stream = new MemoryStream(photo.Image);

                    // identify faces in the image (an image could have multiple faces in it)
                    var faces = await faceClient.DetectAsync(stream);

                    if (faces.Length > 0)
                    {
                        // match each face to the training group photos.  
                        var identifyResult = await faceClient.IdentifyAsync(PersonGroupID, faces.Select(ff => ff.FaceId).ToArray());
                        for (int idx = 0; idx < faces.Length; idx++)
                        {
                            var res = identifyResult[idx];
                            if (res.Candidates.Length > 0)
                            {
                                // found a match so add the original ID of the training person to the photo
                                if (TrainingPhotos.Keys.Contains(res.Candidates[0].PersonId))
                                {
                                    photo.PeopleInPhoto.Add(TrainingPhotos[res.Candidates[0].PersonId]);
                                    photo.NumberOfMatchedFaces += 1;
                                }
                                // didn't find a match so count as an unmatched face.
                                else
                                    photo.NumberOfUnmatchedFaces += 1;
                            }
                            // didn't find a match so count as an unmatched face.
                            else
                                photo.NumberOfUnmatchedFaces += 1;

                        }
                    }

                }


            }
            catch (ClientException ex)
            {
                throw;
            }



        }

        /// <summary>
        /// Add photos to the training group using Microsoft Face API
        /// </summary>
        /// <param name="Photos">List of photos to add</param>
        /// <param name="PersonGroupID">Name of the training group</param>
        /// <returns></returns>
        public async Task addPhotosToTrainingGroup(Dictionary<string, PhotoPerson> Photos, string PersonGroupID)
        {
            IFaceServiceClient faceClient = new FaceServiceClient(SubscriptionKey);

            // Get the group and add photos to the group.
            // The input dictionary is organized by person ID.  The output dictionary is organized by the GUID returned by the added photo from the API.
            try
            {
                await faceClient.GetPersonGroupAsync(PersonGroupID);

                // training photos can support multiple pictures per person (more pictures will make the training more effective).  
                // each photo is added as a Face object within the Face API and attached to a person.

                foreach (PhotoPerson person in Photos.Values)
                {
                    Person p = new Person();
                    p.Name = person.Name;
                    p.PersonId = Guid.NewGuid();

                    List<Guid> faceIDs = new List<Guid>();

                    
                    foreach (Photo photo in person.Photos)
                    {
                        Stream stream = new MemoryStream(photo.Image);
                        Face[] face = await faceClient.DetectAsync(stream);

                        // check for multiple faces - should only have one for a training set.
                        if (face.Length != 1)
                            throw new FaceDetectionException("Expected to detect 1 face but found " + face.Length + " faces for person " + p.Name);
                        else
                            faceIDs.Add(face[0].FaceId);
                    }

                    Guid[] faceIDarray = faceIDs.ToArray();

                    // create the person in the training group with the image array of faces.
                    CreatePersonResult result = await faceClient.CreatePersonAsync(PersonGroupID, faceIDarray, p.Name, null);
                    p.PersonId = result.PersonId;
                    TrainingPhotos.Add(p.PersonId, person);

                }

                await faceClient.TrainPersonGroupAsync(PersonGroupID);
                // Wait until train completed
                while (true)
                {
                    await Task.Delay(1000);
                    var status = await faceClient.GetPersonGroupTrainingStatusAsync(PersonGroupID);
                    if (status.Status != "running")
                    {
                        break;
                    }
                }
            }
            catch (ClientException ex)
            {
                throw;
            }



        }


    }
}
