using Contracts;
using IS.DbCommon;
using IS.DbCommon.Models;
using IS.DbCommon.Models.DTO;
using IS.ImageService.Api.Services.CacheService;
using IS.ImageService.Api.Services.DeterminationService;
using IS.ImageService.Api.Services.FilterService;
using IS.ImageService.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Linq;
using System.Text.Json;
using CoenM.ImageHash.HashAlgorithms;
using Contracts.Serialization;
using CoenM.ImageHash;
using IS.ImageService.Api.Services.TaskPublisherService;

namespace Api.Controllers;

[ApiController]
[Route("images")]
public class ImagesController : ControllerBase
{
    private readonly ILogger<ImagesController> _logger;
    private readonly ITaskPublisher _taskPublisher;
    private readonly IFilterImages _filterService;
    private readonly IFileDeterminator _fileDeterminator;
    private readonly ICacher _cacher;
    private readonly IConfiguration _configuration;
    private readonly ImageServerEFContext _context;

    public ImagesController(
        ILogger<ImagesController> logger,
        ITaskPublisher taskPublisher,
        IFilterImages filterService,
        IFileDeterminator fileDeterminator,
        ICacher cacher,
        IConfiguration configuration,
        ImageServerEFContext context)
    {
        _logger = logger;
        _taskPublisher = taskPublisher;
        _filterService = filterService;
        _fileDeterminator = fileDeterminator;
        _cacher = cacher;
        _configuration = configuration;
        _context = context;
    }

    [HttpGet]
    public IActionResult Get()
    {
        // Observer Logic
        _logger.Log(LogLevel.Information, "Someone is trying to access root images api route");
        return Ok("Logged");
    }

    [HttpGet("count")]
    public IActionResult GetCount()
    {
        // Return images count
        _logger.Log(LogLevel.Information, "Someone is trying to access /count images api route");
        return NotFound("Wrong route! Don't even try to break my system up, cuz i know u're here already.");
    }

    [HttpGet("image/set/{pageNumber}/{count}")]
    public async Task<IActionResult> GetImageSet(
        int? count,
        int? pageNumber,
        CancellationToken ct)
    {
        var filtersQueryDictionary = Request.Query
            .Where(kvp => kvp.Key.StartsWith("filter"))
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToArray().First()
            );

        var imagesList = _filterService
            .FilterPlato(_context.Images, filtersQueryDictionary)
            .Take((count ?? 50) * (pageNumber ?? 1))
            .ToList();

        var dtoList = imagesList
            .Select(img => Mapper.ToDto<ImageSetDto, IS.DbCommon.Models.Image>(img))
            .ToList();

        _logger.Log(LogLevel.Information, "Someone is trying to access /image/set/{count} images api route");

        return Ok(dtoList);
    }

    [HttpGet("image/status/{queueId}")]
    public IActionResult GetImageStatus(long queueId)
    {
        // TODO: Implement status check logic
        return Ok();
    }

    // Дописать логику загрузки изображения:
    // 1) Сохранение файла в файловую систему +
    // 2) В БД записываем базовую информацию об изображении +
    // 3) Публикуем задачу на обработку изображения в RabbitMQ для обработки ИИшкой +
    // 4) Публикуем задачу на добавление метаданных в БД в RabbitMQ
    // 5) Возвращаем клиенту ID задачи в очереди для отслеживания статуса +
    // Что-то придумать с авторизацией: либо реализовать, либо какой-то плейсхолдер оставить

    [HttpPost("load")]
    public async Task<IActionResult> LoadImage(
        [FromForm] UploadImageDto dto,
        CancellationToken ct)
    {
        await using var stream = dto.File.OpenReadStream();
        using var image = await SixLabors.ImageSharp.Image.LoadAsync(stream, ct);

        var imageEntity = new IS.DbCommon.Models.Image
        {
            FileName = dto.File.FileName,
            PerceptualHash = new AverageHash().Hash(stream).ToString(),
            CreatedAt = DateTime.UtcNow
        };

        var metadataEntity = new IS.DbCommon.Models.Metadata();

        var savePath = _configuration["Storage:Root"];
        var (relativePath, sha256Hex) = (string.Empty, string.Empty);

        try
        {
            (relativePath, sha256Hex) = await _fileDeterminator.SaveFileAsync(
                input: stream,
                storageRoot: savePath!,
                tempDir: null,
                extension: Path.GetExtension(dto.File.FileName),
                ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return StatusCode(500);
        }

        imageEntity.RelativePath = relativePath;
        imageEntity.Metadata = metadataEntity;

        var dbImageEntity = await _context.Images.AddAsync(imageEntity, ct);
        await _context.Metadata.AddAsync(metadataEntity, ct);

        await _context.SaveChangesAsync(ct);

        var jobId = await _taskPublisher.PublishToQueueAsync(
            RBQ_Queues.ProcessImage,
            JsonSerializer.SerializeToUtf8Bytes(
                new Contracts.Messages.ProcessImage(relativePath, dbImageEntity.Entity.Id),
                MessageJsonContext.Default.ProcessImage),
            ct);

        await _cacher.SetCacheValueAsync(
            key: $"image:status:{jobId}",
            value: RedisJobsCommon.Processing,
            ct: ct);

        return Ok(new { QueueId = jobId });
    }
}
