using IS.DbCommon;
using IS.DbCommon.Models;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace IS.ImageService.Api.Services.FilterService
{
    public class FilterImages : IFilterImages
    {
        public IQueryable<Image> FilterPlato(IQueryable<Image> images, Dictionary<string, string?>? query)
        {
            if(query == null || query.Count == 0)
            {
                return images.OrderByDescending(img => img.CreatedAt);
            }

            foreach (var kvp in query)
            {
                var keyName = kvp.Key;
                var keyValue = kvp.Value;

                if (string.IsNullOrEmpty(keyValue))
                {
                    continue;
                }

                images = DefineMethod<ImageServerEFContext>(images, keyName, keyValue);
            }

            return images;
        }

        private  IQueryable<Image> DefineMethod<TContext>(IQueryable<Image> q, string keyName, string keyValue)
        {
            switch (keyName) 
            { 
                case "filterByTags":
                    var tags = keyValue.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    q = FilterByTags<TContext>(q, tags);
                    break;
                case "filterByLocation":
                    q = FilterByLocation<TContext>(q, keyValue);
                    break;
                case "orderByPublishTime":
                    q = OrderByPublishTime<TContext>(q, keyValue.Equals("true", StringComparison.OrdinalIgnoreCase));
                    break;
                case "orderByTakenTime":
                    q = OrderByTakenTime<TContext>(q, keyValue.Equals("true", StringComparison.OrdinalIgnoreCase));
                    break;
                default:
                    break;
            }

            return q;
        }

        private  IQueryable<Image> FilterByTags<TContext>(IQueryable<Image> q, string[] tags)
        {
            if(tags == null || tags.Length == 0)
            {
                return q;
            }

            return q.Where(img => img.Labels != null && img.Labels.Any(l => tags.Contains(l))).OrderByDescending(img => img.Labels!.Count(l => tags.Contains(l)));
        }
        private  IQueryable<Image> FilterByLocation<TContext>(IQueryable<Image> q, string location)
        {
            location = location.ToLowerInvariant();

            return FilterAny<Image, string?>(q, img => img.Metadata.LocationCountry == location || img.Metadata.LocationCity == location);
        }

        private  IQueryable<Image> OrderByPublishTime<TContext>(IQueryable<Image> q, bool isByDescending = true)
        {
            return OrderOrThenBy<Image, DateTime>(q, img => img.CreatedAt, isByDescending);
        }
        private  IQueryable<Image> OrderByTakenTime<TContext>(IQueryable<Image> q, bool isByDescending = true)
        {
            return OrderOrThenBy<Image, DateTime?>(q, img => img.Metadata.TakenAtUtc, isByDescending);
        }



        private  IQueryable<T> OrderOrThenBy<T, TKey>(
            IQueryable<T> q,
            Expression<Func<T, TKey>> keySelector,
            bool descending = false)
        {
            if (q is IOrderedQueryable<T> ordered)
                return descending ? ordered.ThenByDescending(keySelector) : ordered.ThenBy(keySelector);

            return descending ? q.OrderByDescending(keySelector) : q.OrderBy(keySelector);
        }
        private  IQueryable<T> FilterAny<T, TKey>(
            IQueryable<T> q, 
            Expression<Func<T, TKey>> keySelector)
        {
            return q.Where(item => keySelector.Compile().Invoke(item) != null);
        }
        private  IQueryable<T> FilterAny<T, TKey>(
            IQueryable<T> q,
            Expression<Func<T, bool>> predicate)
        {
            return q.Where(predicate);
        }
    }

}
