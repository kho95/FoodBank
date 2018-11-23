using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodBank.Models;
using FoodBank.Helpers;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;

namespace FoodBank.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FoodItemsController : ControllerBase
    {
        private readonly FoodBankContext _context;
        private IConfiguration _configuration;

        public FoodItemsController(FoodBankContext context, IConfiguration configuration)
        {
            _context = context; _context = context;
            _configuration = configuration;
        }

        // GET: api/FoodItems
        [HttpGet]
        public IEnumerable<FoodItem> GetFoodItem()
        {
            return _context.FoodItem;
        }

        // GET: api/FoodItems/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetFoodItem([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var foodItem = await _context.FoodItem.FindAsync(id);

            if (foodItem == null)
            {
                return NotFound();
            }

            return Ok(foodItem);
        }

        // PUT: api/FoodItems/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutFoodItem([FromRoute] int id, [FromBody] FoodItem foodItem)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != foodItem.Id)
            {
                return BadRequest();
            }

            _context.Entry(foodItem).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FoodItemExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/FoodItems
        [HttpPost]
        public async Task<IActionResult> PostFoodItem([FromBody] FoodItem foodItem)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.FoodItem.Add(foodItem);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetFoodItem", new { id = foodItem.Id }, foodItem);
        }

        // DELETE: api/FoodItems/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFoodItem([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var foodItem = await _context.FoodItem.FindAsync(id);
            if (foodItem == null)
            {
                return NotFound();
            }

            _context.FoodItem.Remove(foodItem);
            await _context.SaveChangesAsync();

            return Ok(foodItem);
        }

        private bool FoodItemExists(int id)
        {
            return _context.FoodItem.Any(e => e.Id == id);
        }

        // GET: api/FoodItemz/Tags
        [Route("tags")]
        [HttpGet]
        public async Task<List<FoodItem>> GetTagsItem([FromQuery] string tags)
        {
            var foods = from m in _context.FoodItem
                        select m; //get all the memes


            if (!String.IsNullOrEmpty(tags)) //make sure user gave a tag to search
            {
                foods = foods.Where(s => s.Tags.ToLower().Contains(tags.ToLower()) || s.Title.ToLower().Contains(tags.ToLower())); // find the entries with the search tag and reassign
            }

            var returned = await foods.ToListAsync(); //return the memes

            return returned;
        }

         [HttpPost, Route("upload")]
        public async Task<IActionResult> UploadFile([FromForm]FoodImageItem food)
        {
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                return BadRequest($"Expected a multipart request, but got {Request.ContentType}");
            }
            try
            {
                using (var stream = food.Image.OpenReadStream())
                {
                    var cloudBlock = await UploadToBlob(food.Image.FileName, null, stream);
                    //// Retrieve the filename of the file you have uploaded
                    //var filename = provider.FileData.FirstOrDefault()?.LocalFileName;
                    if (string.IsNullOrEmpty(cloudBlock.StorageUri.ToString()))
                    {
                        return BadRequest("An error has occured while uploading your file. Please try again.");
                    }

                    FoodItem foodItem = new FoodItem();
                    foodItem.Title = food.Title;
                    foodItem.Tags = food.Tags;

                    System.Drawing.Image image = System.Drawing.Image.FromStream(stream);
                    foodItem.Height = image.Height.ToString();
                    foodItem.Width = image.Width.ToString();
                    foodItem.Url = cloudBlock.SnapshotQualifiedUri.AbsoluteUri;
                    foodItem.Uploaded = DateTime.Now.ToString();

                    _context.FoodItem.Add(foodItem);
                    await _context.SaveChangesAsync();

                    return Ok($"File: {food.Title} has successfully uploaded");
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"An error has occured. Details: {ex.Message}");
            }


        }

        private async Task<CloudBlockBlob> UploadToBlob(string filename, byte[] imageBuffer = null, System.IO.Stream stream = null)
        {

            var accountName = _configuration["AzureBlob:name"];
            var accountKey = _configuration["AzureBlob:key"]; ;
            var storageAccount = new CloudStorageAccount(new StorageCredentials(accountName, accountKey), true);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            CloudBlobContainer imagesContainer = blobClient.GetContainerReference("images");

            string storageConnectionString = _configuration["AzureBlob:connectionString"];

            // Check whether the connection string can be parsed.
            if (CloudStorageAccount.TryParse(storageConnectionString, out storageAccount))
            {
                try
                {
                    // Generate a new filename for every new blob
                    var fileName = Guid.NewGuid().ToString();
                    fileName += GetFileExtention(filename);

                    // Get a reference to the blob address, then upload the file to the blob.
                    CloudBlockBlob cloudBlockBlob = imagesContainer.GetBlockBlobReference(fileName);

                    if (stream != null)
                    {                       
                        await cloudBlockBlob.UploadFromStreamAsync(stream);
                    }
                    else
                    {
                        return new CloudBlockBlob(new Uri(""));
                    }

                    return cloudBlockBlob;
                }
                catch (StorageException ex)
                {
                    return new CloudBlockBlob(new Uri(""));
                }
            }
            else
            {
                return new CloudBlockBlob(new Uri(""));
            }

        }

        private string GetFileExtention(string fileName)
        {
            if (!fileName.Contains("."))
                return ""; //no extension
            else
            {
                var extentionList = fileName.Split('.');
                return "." + extentionList.Last(); //assumes last item is the extension 
            }
        }

    }
}