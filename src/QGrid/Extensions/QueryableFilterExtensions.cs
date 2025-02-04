﻿using System;
using System.Linq;
using System.Linq.Expressions;
using QGrid.Enums;
using QGrid.FilterExpressionProviders;
using QGrid.Models;

namespace QGrid.Extensions
{
    public static class QueryableFilterExtensions
    {
        public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query, QGridFilters filters)
            where T : class
        {
            if (filters?.Filters == null)
                return query;

            Expression resultExpression = null;
            var parameterExpression = Expression.Parameter(typeof(T), typeof(T).Name);

            foreach (var filter in filters.Filters)
            {
                var filterExpression = GetFilterExpression<T>(filter, parameterExpression);

                if (resultExpression == null)
                {
                    resultExpression = filterExpression;
                }
                else
                {
                    resultExpression = filters.Operator == FilterOperatorEnum.And
                        ? Expression.AndAlso(resultExpression, filterExpression)
                        : Expression.OrElse(resultExpression, filterExpression);
                }
            }

            if (resultExpression == null)
                return query;

            var lambda = Expression.Lambda<Func<T, bool>>(resultExpression, parameterExpression);
            return query.Where(lambda);
        }

        private static Expression GetFilterExpression<T>(
            QGridFilter filter,
            ParameterExpression entityParameterExpression
        )
        {
            var propertyInfo = filter.Column.GetPropertyInfo<T>();
            BaseFilterExpressionProvider filterExpressionProvider;

            // if null is provided as a filter value we don't care what type of column
            // filter should be applied to
            if (filter.Value == null)
            {
                filterExpressionProvider = new NullValueFilterExpressionProvider(
                    propertyInfo,
                    filter,
                    entityParameterExpression
                );
            }
            else if (propertyInfo.PropertyType == typeof(string))
            {
                filterExpressionProvider = new StringFilterExpressionProvider(
                    propertyInfo,
                    filter,
                    entityParameterExpression
                );
            }
            else if (propertyInfo.PropertyType.IsNumberType())
            {
                filterExpressionProvider = new NumberFilterExpressionProvider(
                    propertyInfo,
                    filter,
                    entityParameterExpression
                );
            }
            else if (propertyInfo.PropertyType.IsDateTimeType())
            {
                filterExpressionProvider = new DateTimeFilterExpressionProvider(
                    propertyInfo,
                    filter,
                    entityParameterExpression
                );
            }
            else if (propertyInfo.PropertyType.IsEnum())
            {
                filterExpressionProvider = new EnumFilterExpressionProvider(
                    propertyInfo,
                    filter,
                    entityParameterExpression
                );
            }
            else if (propertyInfo.PropertyType.IsBoolType())
            {
                filterExpressionProvider = new BoolFilterExpressionsProvider(
                    propertyInfo,
                    filter,
                    entityParameterExpression
                );
            }
            else if (propertyInfo.PropertyType.IsGuidType())
            {
                filterExpressionProvider = new GuidFilterExpressionsProvider(
                    propertyInfo,
                    filter,
                    entityParameterExpression
                );
            }
            else
            {
                throw new ArgumentOutOfRangeException(
                    nameof(propertyInfo.PropertyType),
                    propertyInfo.PropertyType,
                    $"Attempted to filter by column with an unsupported type of {propertyInfo.PropertyType}"
                );
            }

            return filterExpressionProvider.GetFilterExpression();
        }
    }
}