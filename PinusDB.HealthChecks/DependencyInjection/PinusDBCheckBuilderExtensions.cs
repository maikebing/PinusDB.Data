﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Maikebing.HealthChecks.Taos;

namespace Microsoft.Extensions.DependencyInjection
{


    public static class PinusDBCheckBuilderExtensions
    {
        internal const string HEALTH_QUERY = "select  count(*) as status  from sys_connection";
        private const string NAME = "PinusDB";

        /// <summary>
        /// Add a health check for TDengine services.
        /// </summary>
        /// <param name="builder">The <see cref="IHealthChecksBuilder"/>.</param>
        /// <param name="connectionString">The Sql Server connection string to be used.</param>
        /// <param name="healthQuery">The query to be executed.Optional. If <c>null</c> select 1 is used.</param>
        /// <param name="name">The health check name. Optional. If <c>null</c> the type name 'TDengine' will be used for the name.</param>
        /// <param name="failureStatus">
        /// The <see cref="HealthStatus"/> that should be reported when the health check fails. Optional. If <c>null</c> then
        /// the default status of <see cref="HealthStatus.Unhealthy"/> will be reported.
        /// </param>
        /// <param name="tags">A list of tags that can be used to filter sets of health checks. Optional.</param>
        /// <param name="timeout">An optional System.TimeSpan representing the timeout of the check.</param>
        /// <returns>The <see cref="IHealthChecksBuilder"/>.</returns>
        public static IHealthChecksBuilder AddPinusDB(this IHealthChecksBuilder builder,
            string connectionString,
            string healthQuery = default,
            string name = default,
            HealthStatus? failureStatus = default,
            IEnumerable<string> tags = default,
            TimeSpan? timeout = default)
        {
            return builder.AddPinusDB(_ => connectionString, healthQuery, name, failureStatus, tags, timeout);
        }

        /// <summary>
        /// Add a health check for TDengine services.
        /// </summary>
        /// <param name="builder">The <see cref="IHealthChecksBuilder"/>.</param>
        /// <param name="connectionStringFactory">A factory to build the SQL Server connection string to use.</param>
        /// <param name="healthQuery">The query to be executed.Optional. If <c>null</c> select 1 is used.</param>
        /// <param name="name">The health check name. Optional. If <c>null</c> the type name 'TDengine' will be used for the name.</param>
        /// <param name="failureStatus">
        /// The <see cref="HealthStatus"/> that should be reported when the health check fails. Optional. If <c>null</c> then
        /// the default status of <see cref="HealthStatus.Unhealthy"/> will be reported.
        /// </param>
        /// <param name="tags">A list of tags that can be used to filter sets of health checks. Optional.</param>
        /// <param name="timeout">An optional System.TimeSpan representing the timeout of the check.</param>
        /// <returns>The <see cref="IHealthChecksBuilder"/>.</returns>
        public static IHealthChecksBuilder AddPinusDB(this IHealthChecksBuilder builder,
            Func<IServiceProvider,
                string> connectionStringFactory,
            string healthQuery = default,
            string name = default,
            HealthStatus? failureStatus = default,
            IEnumerable<string> tags = default,
            TimeSpan? timeout = default)
        {
            if (connectionStringFactory == null)
            {
                throw new ArgumentNullException(nameof(connectionStringFactory));
            }

            return builder.Add(new HealthCheckRegistration(
                name ?? NAME,
                sp => new PinusDBHealthCheck(connectionStringFactory(sp), healthQuery ?? HEALTH_QUERY),
                failureStatus,
                tags,
                timeout));
        }
    }
}
