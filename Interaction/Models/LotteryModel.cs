﻿using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Interaction.Models
{
    public class LotteryModel
    {
        public static readonly string LotteryTableName = "LotteryAwards";
        public static readonly string LotteryPhoneName = "LotteryPhoneNumber";

        public enum AwardsCategory { Lottery };

        public void InsertLotteryEntity(AwardEntity entity)
        {
            StorageModel.GetTable(LotteryModel.LotteryTableName)
                        .Execute(TableOperation.Insert(entity));
        }

        public void InsertPhoneNameEntity(PhoneNumberEntity entity)
        {
            StorageModel.GetTable(LotteryModel.LotteryPhoneName)
                        .Execute(TableOperation.Insert(entity));
        }

        public void UpdateLotteryEntity(AwardEntity entity)
        {
            StorageModel.GetTable(LotteryModel.LotteryTableName)
                        .Execute(TableOperation.Replace(entity));
        }

        public void UpdatePhoneNameEntity(PhoneNumberEntity entity)
        {
            StorageModel.GetTable(LotteryModel.LotteryPhoneName)
                        .Execute(TableOperation.Replace(entity));
        }

        public void DeleteLotteryEntity(AwardEntity entity)
        {
            StorageModel.GetTable(LotteryModel.LotteryTableName)
                        .Execute(TableOperation.Delete(entity));
        }

        public AwardEntity GetAwardEntity(string partitionKey, string rowKey)
        {
            TableResult res = StorageModel.GetTable(LotteryModel.LotteryTableName)
                                           .Execute(TableOperation.Retrieve<AwardEntity>(partitionKey, rowKey));

            return (null != res.Result) ? (AwardEntity)res.Result : null;
        }

        public string CreateNewAwardId() 
        {
            return "A" + DateTime.Now.ToString("yyyyMMddHHmmssfff") +
                 "P" + (new Random()).Next(999999).ToString("000000");
        }

        public string CreateNewPhoneNumberId() 
        {
            return "P" + DateTime.Now.ToString("yyyyMMddHHmmssfff") +
                 "P" + (new Random()).Next(999999).ToString("000000");
        }

        public string CapsuleQueryies(List<string> queries) 
        { 
            string splitStr = "$", qstr = ""; 
 
            foreach (string q in queries) 
            {
                qstr = splitStr + q;
            } 
 
            return qstr.TrimStart(new char[] { '$' }); 
        }

        public List<string> ExtractQueries(string qstr)
        {
            return qstr.Split(new char[] { '$' }, StringSplitOptions.RemoveEmptyEntries).ToList<string>();
        }

        public AwardEntity GetAwardByRowId(AwardsCategory category, string RowId) 
        {
            List<AwardEntity> awards = StorageModel.GetTable(LotteryTableName)
                .ExecuteQuery(new TableQuery<AwardEntity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, category.ToString()))
                .Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, RowId)))
                .ToList<AwardEntity>();

            return awards.FirstOrDefault();
        }

        public List<AwardEntity> GetAwardsByCategory(AwardsCategory category)
        {
            List<AwardEntity> awards = StorageModel.GetTable(LotteryTableName)
                .ExecuteQuery(new TableQuery<AwardEntity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, category.ToString())))
                .ToList<AwardEntity>();

            awards.Sort(delegate(AwardEntity aea, AwardEntity aeb)
            {
                if (aea.StartDate < aeb.StartDate)
                    return -1;
                else if (aea.StartDate == aeb.StartDate)
                    return 0;
                else
                    return 1;
            });

            awards.Sort(delegate(AwardEntity aea, AwardEntity aeb)
            {
                if (aea.EndDate < aeb.EndDate)
                    return -1;
                else if (aea.EndDate == aeb.EndDate)
                    return 0;
                else
                    return 1;
            });

            return awards;
        }

        public List<List<PhoneNumberEntity>> GetPhoneNumberOrderbyAward(bool containAll)
        {
            List<AwardEntity> awards = GetAwardsByCategory(AwardsCategory.Lottery);
            awards.Sort(delegate(AwardEntity a, AwardEntity b)
            {
                if (a.StartDate < b.StartDate)
                    return -1;
                else if (a.StartDate == b.StartDate)
                    return 0;
                else
                    return 1;
            });
            List<string> allOfAwards = new List<string>();
            foreach (var award in awards)
            {
                allOfAwards.Add(award.RowKey);
            }

            List<List<PhoneNumberEntity>> result = new List<List<PhoneNumberEntity>>();
            foreach (var RowKey in allOfAwards)
            {
                result.Add(GetPhoneNumberbyAward(RowKey, containAll));
            }
            return result;
        }

        public List<PhoneNumberEntity> GetPhoneNumberbyAwardAndDate(string PartitionKey, DateTime date)
        {
            List<PhoneNumberEntity> phonenumbersTemp = StorageModel.GetTable(LotteryPhoneName)
                .ExecuteQuery(new TableQuery<PhoneNumberEntity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, PartitionKey))
                .Where(TableQuery.GenerateFilterConditionForBool("Validity", QueryComparisons.Equal, true)))
                .ToList<PhoneNumberEntity>();
            List<PhoneNumberEntity> phonenumbers = new List<PhoneNumberEntity>();
            foreach (var phonenumber in phonenumbersTemp)
            {
                if (phonenumber.InsertTime.ToString("yyyy-MM-dd").Equals(date.ToString("yyyy-MM-dd")))
                {
                    phonenumbers.Add(phonenumber);
                }
            }
            phonenumbers.Sort(delegate(PhoneNumberEntity a, PhoneNumberEntity b)
            {
                if (a.InsertTime < b.InsertTime)
                    return 1;
                else if (a.InsertTime == b.InsertTime)
                    return 0;
                else
                    return -1;
            });

            return phonenumbers;
        }

        public List<PhoneNumberEntity> GetPhoneNumberbyAward(string PartitionKey, bool containAll)
        {
            List<PhoneNumberEntity> phonenumbersTemp = StorageModel.GetTable(LotteryPhoneName)
                .ExecuteQuery(new TableQuery<PhoneNumberEntity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, PartitionKey))).ToList<PhoneNumberEntity>();

            List<PhoneNumberEntity> phonenumbers = new List<PhoneNumberEntity>();
            foreach (var phonenumber in phonenumbersTemp)
            {
                if (containAll == true)
                    phonenumbers.Add(phonenumber);
                else
                {
                    if (phonenumber.Validity == true)
                        phonenumbers.Add(phonenumber);
                }
            }

            phonenumbers.Sort(delegate(PhoneNumberEntity a, PhoneNumberEntity b)
            {
                if (a.InsertTime < b.InsertTime)
                    return 1;
                else if (a.InsertTime == b.InsertTime)
                    return 0;
                else
                    return -1;
            });

            return phonenumbers;
        }

        public bool IdentifyPhoneNumber(string number)
        {
            if (number.Length != 11)
                return false;
            char[] charArray = number.ToCharArray();
            if (charArray[0] != '1')
                return false;
            else
            {
                foreach (var c in charArray)
                {
                    if (c > '9' || c < '0')
                        return false;
                }
                return true;
            }
        }

        public bool IdentifyDuplicatePhoneNumber(string number)
        {
            List<PhoneNumberEntity> numbers = StorageModel.GetTable(LotteryPhoneName)
                .ExecuteQuery(new TableQuery<PhoneNumberEntity>()
                .Where(TableQuery.GenerateFilterCondition("PhoneNumber", QueryComparisons.Equal, number)))
                .ToList<PhoneNumberEntity>();
            if (numbers.Count() == 0)
                return false;
            else
                return true;
        }
    }

    public class AwardEntity : TableEntity
    {
        public string AwardName { get; set; }

        public string Url { set; get; }

        public double AwardRate { set; get; }

        public long AwardQuota { set; get; }

        public long TotalVolume { set; get; }

        public DateTime StartDate { set; get; }

        public DateTime EndDate { set; get; }

        public long PVCount { set; get; }

        public string TriggerQueries { get; set; }

        public AwardEntity() { }

        public AwardEntity(string PartitionKey, string AwardId)
        {
            this.PartitionKey = PartitionKey;
            this.RowKey = AwardId;
        }
    }

    public class PhoneNumberEntity : TableEntity
    {
        public PhoneNumberEntity() { }

        public PhoneNumberEntity(string PartitionKey, string RowId)
        {
            this.PartitionKey = PartitionKey;
            this.RowKey = RowId;
        }

        public string PhoneNumber { set; get; }

        public string AwardName { set; get; }

        public DateTime InsertTime { set; get; }

        public Boolean Validity { set; get; }
    }

    public class AwardNameDropDownListViewModel
    {
        [Display(Name = "奖品名称")]
        public int awardRowKey { get; set; }
        public IEnumerable<SelectListItem> award { get; set; }
    }

    public class LotteryAnswerData
    {
        public string AwardId { get; set; }

        public string TriggerQueries { get; set; }

        public string Status { get; set; }
    }

    public class CheckResult 
    {
        public string AwardName { set; get; }

        public long OldNumber { set; get; }

        public long ReviseNumber { set; get; }

        public CheckResult(string AwardName, long OldNumber, long ReviseNumber) 
        {
            this.AwardName = AwardName;
            this.OldNumber = OldNumber;
            this.ReviseNumber = ReviseNumber;
        }
    }
}
