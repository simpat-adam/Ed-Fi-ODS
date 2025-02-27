﻿// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.Common.Metadata.Profiles;
using EdFi.Ods.Common.Profiles;
using log4net;
using MediatR;

namespace EdFi.Ods.Api.Security.Profiles
{
    public class AdminProfileNamesPublisher : IAdminProfileNamesPublisher, INotificationHandler<ProfileMetadataCacheExpired>
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof(AdminProfileNamesPublisher));
        private readonly IProfileResourceNamesProvider _profileResourceNamesProvider;
        private readonly IUsersContextFactory _usersContextFactory;

        public AdminProfileNamesPublisher(
            IUsersContextFactory usersContextFactory,
            IProfileResourceNamesProvider profileResourceNamesProvider)
        {
            _profileResourceNamesProvider = profileResourceNamesProvider ?? throw new ArgumentNullException(nameof(profileResourceNamesProvider));
            _usersContextFactory = usersContextFactory ?? throw new ArgumentNullException(nameof(usersContextFactory));
        }

        public async Task<bool> PublishProfilesAsync()
        {
            return await Task.Factory.StartNew(PublishProfiles);
        }

        private bool PublishProfiles()
        {
            try
            {
                //get the set of profiles from any Profiles.xml files found in assemblies
                var definedProfiles = _profileResourceNamesProvider.GetProfileResourceNames()
                    .Select(x => x.ProfileName)
                    .Distinct();

                using (var usersContext = _usersContextFactory.CreateContext())
                {
                    //determine which Profiles from the Profiles.xml files do not exist in the admin database
                    var publishedProfiles = usersContext.Profiles.Select(x => x.ProfileName).ToList();

                    var profilesToInsert = definedProfiles.Except(publishedProfiles).ToList();

                    //if there are none to insert return
                    if (!profilesToInsert.Any())
                    {
                        return true;
                    }

                    if (_logger.IsDebugEnabled)
                    {
                        _logger.Debug($"Publishing the following Profile names to the Admin database: {string.Join("', '", profilesToInsert)}");
                    }

                    // for each profile not in the database, add it
                    foreach (var profileName in profilesToInsert)
                    {
                        usersContext.Profiles.Add(new Profile { ProfileName = profileName });
                    }

                    usersContext.SaveChanges();
                }

                return true;
            }
            catch (Exception exception)
            {
                //If an exception occurs log it and return false since it is an async call.
                _logger.Error("An error occured when attempting to publish Profiles to the admin database.", exception);

                return false;
            }
        }

        public async Task Handle(ProfileMetadataCacheExpired notification, CancellationToken cancellationToken)
        {
            if (_logger.IsDebugEnabled)
            {
                _logger.Debug("Reevaluating Profiles for possible publishing to Admin database due to profile metadata cache expiration...");
            }

            await PublishProfilesAsync();
        }
    }
}
