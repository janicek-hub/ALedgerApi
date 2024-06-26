﻿using ALedgerApi.Model;
using RestDWH.Base.Model;
using System.Security.Claims;

namespace ALedgerApi.Events
{
    public class AddressEvents : RestDWHEvents<Address>
    {
        public override async Task<(int from, int size, string query, string sort)> BeforeGetAsync(int from = 0, int size = 10, string query = "*", string sort = "", ClaimsPrincipal? user = null)
        {
            return (from, size, query, sort);
        }

        public override async Task<DBBase<Address>> ToCreate(DBBase<Address> item, ClaimsPrincipal? user = null)
        {
            return item;
        }

        public override async Task<DBBase<Address>> AfterDeleteAsync(DBBase<Address> result, string id, ClaimsPrincipal? user = null)
        {
            return result;
        }
    }
}
