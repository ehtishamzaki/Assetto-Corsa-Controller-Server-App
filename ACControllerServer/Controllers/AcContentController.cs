using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace ACControllerServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AcContentController : ControllerBase
    {

        private readonly ILogger<AcContentController> _logger;
        private readonly IAcDirectoryService _AcContentService;

        public AcContentController(ILogger<AcContentController> logger, 
            IAcDirectoryService acContentService)
        {
            _logger = logger;
            _AcContentService = acContentService;
        }

        [HttpGet("cars")]
        public IActionResult GetCars()
        {
            return Ok(_AcContentService.Cars);
        }

        [HttpGet("tracks")]
        public IActionResult GetTracks()
        {
            return Ok(_AcContentService.Tracks);
        }

        [HttpGet("carimage")]
        public async Task<IActionResult> GetCarImageAsync(string carId, string skinId)
        {
            var skin = _AcContentService.Cars.Find(car => car.CarId == carId)
                .Skins.Find(skin => skin.SkinId == skinId);
            MemoryStream memory = new MemoryStream();
            using (FileStream stream = new(skin.SkinPreviewImage, FileMode.Open))
                await stream.CopyToAsync(memory);
            memory.Position = 0;
            return File(memory, "image/jpg", Path.GetFileName(skin.SkinPreviewImage));
        }

        [HttpGet("trackoutline")]
        public async Task<IActionResult> GetTrackOutlineAsync(string trackId, string layoutId)
        {
            var layout = _AcContentService.Tracks.Find(track => track.TrackId == trackId)
                .Layouts.Find(layout => layout.Id == layoutId);
            MemoryStream memory = new MemoryStream();
            using (FileStream stream = new(layout.OutlineMapImage, FileMode.Open))
                await stream.CopyToAsync(memory);
            memory.Position = 0;
            return File(memory, "image/png", Path.GetFileName(layout.OutlineMapImage));
        }

        [HttpGet("trackpreview")]
        public async Task<IActionResult> GetTrackPreviewAsync(string trackId, string layoutId)
        { 
            var layout = _AcContentService.Tracks.Find(track => track.TrackId == trackId)
                .Layouts.Find(layout => layout.Id == layoutId);
            MemoryStream memory = new MemoryStream();
            using (FileStream stream = new(layout.ThumbnailImage, FileMode.Open))
                await stream.CopyToAsync(memory);
            memory.Position = 0;
            return File(memory, "image/png", Path.GetFileName(layout.ThumbnailImage));
        }

    }
}
