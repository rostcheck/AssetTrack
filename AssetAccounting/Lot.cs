namespace AssetAccounting
{
	public class Lot
	{
		public string LotID { get; set; }
		public DateTime PurchaseDate { get; set; }
		public DateTime? CloseDate
		{
			get
            {
				return closeDate;
            }
		}
		public decimal OriginalAmount { get; set; }
		public ValueInCurrency OriginalPrice { get; set; }
		public ValueInCurrency AdjustedPrice
		{ 
			get
			{
				return adjustedPrice;
			}
		}
		public AssetMeasurementUnitEnum measurementUnit { get; set; }
		public AssetTypeEnum AssetType { get; set; }	
		public string ItemType { get; set; }
		public string Vault 
		{ 
			get
			{
				return vault;
			}
			set
			{
				vault = value;
				history.Add("Set vault to " + value);
			}
		}

		public List<string> History 
		{ 
			get
			{
				return history;
			}
		}

		public string Account
		{
			get
			{
				return account;
			}
			set
			{
				account = value;
				history.Add("Set account to " + value);
			}
		}

		public string Service
		{
			get
			{
				return service;
			}
			set
			{
				service = value;
                history.Add("Transferred lot to " + value);
            }
		}

		private decimal currentAmount;
		private List<string> history;
		private string vault;
		private string account;
		private string service;
		private ValueInCurrency adjustedPrice;
		private DateTime? closeDate = null;

		public Lot(string service, string transactionID, DateTime purchaseDate, decimal originalAmount, AssetMeasurementUnitEnum measurementUnit, 
			ValueInCurrency price, AssetTypeEnum assetType, string vault, string account, string itemType)
		{
			history = new List<string>();
			this.service = service;
			this.LotID = transactionID;
			this.PurchaseDate = purchaseDate;
			this.OriginalAmount = originalAmount;
			this.currentAmount = originalAmount;
			this.measurementUnit = measurementUnit;
			this.OriginalPrice = price;
			this.AssetType = assetType;
			this.adjustedPrice = new ValueInCurrency(OriginalPrice);
			this.vault = vault;
			this.account = account;
			this.ItemType = itemType;
			history.Add(string.Format("{0} Opened lot: bought {1} {2} {3} ({4}) for {5} {6}, vault {7}, account {8}", 
				purchaseDate.ToShortDateString(), originalAmount, measurementUnit, assetType, itemType, price.Value, 
				price.Currency, vault, account));
		}

		// Get the current amount expressed in specified units
		public decimal CurrentAmount(AssetMeasurementUnitEnum toUnit)
		{
			return Utils.ConvertMeasurementUnit(currentAmount, this.measurementUnit, toUnit);
		}

		public bool IsDepleted()
        {
			return currentAmount == 0.0m; // Current measure is 0 (in native units)
        }

		public void ApplyFeeInCurrency(ValueInCurrency fee)
		{
			this.AdjustedPrice.Value += Utils.ConvertCurrency(fee.Value, fee.Currency, this.AdjustedPrice.Currency);
			history.Add(fee.Date.ToShortDateString() + " Applied fee " + fee.Value + " " + fee.Currency);
		}

		public void DecreaseAmountViaFee(DateTime transactionDateTime, decimal amount, AssetMeasurementUnitEnum measurementUnit)
        {
			this.DecreaseAmount(transactionDateTime, amount, measurementUnit);
            history.Add(string.Format("{0} Decreased by {1:0.0000000} {2} as fee", transactionDateTime.Date.ToShortDateString(),
				amount, this.measurementUnit));
		}

		public void DecreaseMeasureViaTransfer(DateTime transactionDateTime, decimal measurementAmount, AssetMeasurementUnitEnum fromMeasurementUnit,
			string account, string vault)
		{
			this.DecreaseAmount(transactionDateTime, measurementAmount, fromMeasurementUnit);
			history.Add(string.Format("{0} Transferred {1:0.0000000} {2} to account {3}, vault {4}", transactionDateTime.Date.ToShortDateString(),
				measurementAmount, measurementUnit, account, vault));
		}

		private void DecreaseAmount(DateTime transactionDateTime, decimal measurementAmount, AssetMeasurementUnitEnum fromMeasurementUnit)
		{
			decimal newCurrentMeasure = currentAmount - Utils.ConvertMeasurementUnit(measurementAmount, fromMeasurementUnit, this.measurementUnit);
			if (newCurrentMeasure < 0.0m)
				throw new Exception("Cannot decrease lot measure by more than its current mesure");
			currentAmount = newCurrentMeasure;
			if (IsDepleted())
				closeDate = transactionDateTime;
		}

		private void IncreaseMeasure(decimal amount, AssetMeasurementUnitEnum measurementUnit)
		{
			currentAmount += Utils.ConvertMeasurementUnit(amount, measurementUnit, this.measurementUnit);
            history.Add(string.Format("Increased by {0:0.0000000} {1}", amount, this.measurementUnit));
		}			

		public TaxableSale Sell(AssetAmount amount, ValueInCurrency salePrice)
		{
			if (this.AssetType != amount.AssetType)
				throw new Exception("Asset types in Sell() do not match: lot type " + this.AssetType + ", sale type " + amount.AssetType);

			if (this.ItemType != amount.ItemType)
				throw new Exception("Item types in Sell() do not match: lot type " + this.ItemType + ", sale type " + amount.ItemType);
			
			decimal unitsToSell = Utils.ConvertMeasurementUnit(amount.Measure, amount.MeasurementUnit, this.measurementUnit);
			decimal percentOfLotToSell = unitsToSell / currentAmount;

			decimal newCurrentAmount = currentAmount - unitsToSell;
			if (newCurrentAmount < 0.0m)
				throw new Exception("Cannot sell more than the lot's current measure");
			if (IsDepleted())
				closeDate = salePrice.Date;

			TaxableSale taxableSale = new TaxableSale(this, amount, salePrice);
			currentAmount = newCurrentAmount;
			AdjustedPrice.Value = AdjustedPrice.Value * (1.0m - percentOfLotToSell);
			history.Add(string.Format("{0} Sold {1} {2} {3} for {4} {5:0.00}", 
				salePrice.Date.ToShortDateString(), amount.Measure, amount.MeasurementUnit, amount.AssetType,
				salePrice.Currency, salePrice.Value));
			return taxableSale;
		}

		public AssetAmount GetAmountToSell(AssetAmount desiredAmount)
		{
			if (this.AssetType != desiredAmount.AssetType)
				throw new Exception("Asset types in GetAmountToSell() do not match: lot type " + this.AssetType + ", sale type " + desiredAmount.AssetType);

			if (this.ItemType != desiredAmount.ItemType)
				throw new Exception("Item types in GetAmountToSell() do not match: lot type " + this.ItemType + ", sale type " + desiredAmount.ItemType);

			decimal desiredMeasure = Utils.ConvertMeasurementUnit(desiredAmount.Measure, desiredAmount.MeasurementUnit, this.measurementUnit);
			return new AssetAmount(currentAmount > desiredMeasure ? desiredMeasure : currentAmount, this.AssetType, this.measurementUnit, this.ItemType);
		}
	}
}

