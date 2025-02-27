﻿// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Configuration;
using System.Data.Entity;
using System.Transactions;
using EdFi.Common;

namespace EdFi.Admin.DataAccess.Utils
{
    public class ValidateDatabase<TContext> : IDatabaseInitializer<TContext>
        where TContext : DbContext
    {
        public void InitializeDatabase(TContext context)
        {
            Preconditions.ThrowIfNull(context, nameof(context));

            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                if (!context.Database.Exists())
                {
                    throw new ConfigurationErrorsException("Unable to open connection to the Admin database.");
                }
            }
        }
    }
}
