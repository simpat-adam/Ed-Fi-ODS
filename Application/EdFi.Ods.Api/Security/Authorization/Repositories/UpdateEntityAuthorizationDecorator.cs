﻿// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using EdFi.Ods.Api.Security.Authorization.Filtering;
using EdFi.Ods.Api.Security.AuthorizationStrategies.Relationships.Filters;
using EdFi.Ods.Common;
using EdFi.Ods.Common.Context;
using EdFi.Ods.Common.Extensions;
using EdFi.Ods.Common.Infrastructure.Filtering;
using EdFi.Ods.Common.Repositories;
using EdFi.Ods.Common.Security;
using EdFi.Ods.Common.Security.Claims;
using EdFi.Security.DataAccess.Repositories;
using NHibernate;

namespace EdFi.Ods.Api.Security.Authorization.Repositories
{
    /// <summary>
    /// Authorizes calls to the "Update" repository method.
    /// </summary>
    /// <typeparam name="T">The Type of entity being updated.</typeparam>
    public class UpdateEntityAuthorizationDecorator<T>
        : RepositoryOperationAuthorizationDecoratorBase<T>,
          IUpdateEntity<T>
        where T : class, IHasIdentifier, IDateVersionedEntity
    {
        private readonly IUpdateEntity<T> _next;
        private readonly ISecurityRepository _securityRepository;
        private readonly IViewBasedSingleItemAuthorizationQuerySupport _viewBasedSingleItemAuthorizationQuerySupport;

        /// <summary>
        /// Initializes a new instance of UpdateEntityAuthorizationDecorator.
        /// </summary>
        /// <param name="next">The decorated instance for which authorization is being performed.</param>
        /// <param name="securityRepository">Provides access to the repository where the claims/actions are stored.</param>
        /// <param name="authorizationContextProvider">Provides access to the authorization context, such as the resource and action.</param>
        /// <param name="authorizationFilteringProvider"></param>
        /// <param name="authorizationFilterDefinitionProvider"></param>
        /// <param name="explicitObjectValidators"></param>
        /// <param name="authorizationBasisMetadataSelector"></param>
        /// <param name="sessionFactory"></param>
        /// <param name="apiKeyContextProvider"></param>
        /// <param name="viewBasedSingleItemAuthorizationQuerySupport"></param>
        /// <param name="dataManagementResourceContextProvider"></param>
        public UpdateEntityAuthorizationDecorator(
            IUpdateEntity<T> next,
            ISecurityRepository securityRepository,
            IAuthorizationContextProvider authorizationContextProvider,
            IAuthorizationFilteringProvider authorizationFilteringProvider,
            IAuthorizationFilterDefinitionProvider authorizationFilterDefinitionProvider,
            IExplicitObjectValidator[] explicitObjectValidators,
            IAuthorizationBasisMetadataSelector authorizationBasisMetadataSelector,
            ISessionFactory sessionFactory,
            IApiKeyContextProvider apiKeyContextProvider,
            IViewBasedSingleItemAuthorizationQuerySupport viewBasedSingleItemAuthorizationQuerySupport,
            IContextProvider<DataManagementResourceContext> dataManagementResourceContextProvider)
            : base(
                    authorizationContextProvider,
                    authorizationFilteringProvider,
                    authorizationFilterDefinitionProvider,
                    explicitObjectValidators,
                    authorizationBasisMetadataSelector,
                    securityRepository,
                    sessionFactory,
                    apiKeyContextProvider,
                    viewBasedSingleItemAuthorizationQuerySupport,
                    dataManagementResourceContextProvider)
        {
            _next = next;
            _securityRepository = securityRepository;
            _viewBasedSingleItemAuthorizationQuerySupport = viewBasedSingleItemAuthorizationQuerySupport;
        }

        /// <summary>
        /// Authorizes a call to get a update an entity.
        /// </summary>
        /// <param name="persistentEntity">An entity instance that has all the primary key properties assigned with values.</param>
        /// <returns>The specified entity if found; otherwise null.</returns>
        public async Task UpdateAsync(T persistentEntity, CancellationToken cancellationToken)
        {
            // POST comes in as an "Upsert", but at this point we know it's actually about to update an entity,
            // so we'll use the more explicit action for authorization.
            var updateActionUri = _securityRepository.GetActionByName("Update").ActionUri;

            // Authorize the request
            await AuthorizeSingleItemAsync(persistentEntity, updateActionUri, cancellationToken);

            // Pass call through to the repository operation
            await _next.UpdateAsync(persistentEntity, cancellationToken);
        }
    }
}
