using IS.DbCommon.Models;

namespace IS.ImageService.Api.Services.FilterService
{
    public interface IFilterImages
    {
        IQueryable<Image> FilterPlato(
            IQueryable<Image> images,
            Dictionary<string, string?>? query);
    }
}
