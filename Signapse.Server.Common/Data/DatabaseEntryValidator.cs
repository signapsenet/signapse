﻿using Signapse.Services;
using System;
using System.Security.Claims;

namespace Signapse.Data
{
    public interface IDatabaseEntryValidator
    {
        bool ValidateDelete<T>(T item) where T : IDatabaseEntry;
        bool ValidateInsert<T>(T item) where T : IDatabaseEntry;
        bool ValidateUpdate<T>(T item) where T : IDatabaseEntry;
        bool ValidateRead<T>(T item) where T : IDatabaseEntry;
    }

    public class DatabaseEntryValidator<TEntry> : IDatabaseEntryValidator
        where TEntry : class, IDatabaseEntry
    {
        protected readonly IAuthResults authResults;
        protected readonly ClaimsPrincipal user;

        public DatabaseEntryValidator(ClaimsPrincipal user, IAuthResults authResults)
        {
            this.user = user;
            this.authResults = authResults;
        }

        public virtual bool ValidateInsert(TEntry item)
        {
            if (authResults.IsUsersAdmin)
            {
                return true;
            }
            else throw new Exceptions.HttpUnauthorized();
        }

        public virtual bool ValidateDelete(TEntry item)
        {
            if (authResults.IsAuthorized
                && (item.ID == user.SignapseUserID()
                    || authResults.IsUsersAdmin))
            {
                return true;
            }
            else throw new Exceptions.HttpUnauthorized();
        }

        public virtual bool ValidateUpdate(TEntry item)
        {
            if (authResults.IsAuthorized
                && (item.ID == user.SignapseUserID()
                    || authResults.IsUsersAdmin))
            {
                return true;
            }
            else throw new Exceptions.HttpUnauthorized();
        }

        public virtual bool ValidateRead(TEntry item) => true;

        bool IDatabaseEntryValidator.ValidateDelete<T>(T item)
            => this.ValidateDelete(item as TEntry ?? throw new ArgumentException());

        bool IDatabaseEntryValidator.ValidateInsert<T>(T item)
            => this.ValidateInsert(item as TEntry ?? throw new ArgumentException());

        bool IDatabaseEntryValidator.ValidateUpdate<T>(T item)
            => this.ValidateUpdate(item as TEntry ?? throw new ArgumentException());

        bool IDatabaseEntryValidator.ValidateRead<T>(T item)
            => this.ValidateRead(item as TEntry ?? throw new ArgumentException());
    }
}