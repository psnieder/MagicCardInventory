Inventory MTG cards in a local SQL server database. Utilizes the scryfall API to get card information (https://scryfall.com/docs/api).

# Command Line Arguments: 
	```
	0 - Function to run. Current functions are Add and UpdatePrices. Add will inventory a new card, and UpdatePrices will update the prices of all inventoried cards
		Add - command line arguments 1-3 are required
		UpdatePrices - no additional command line arguments are required.

	1 - Exact card name

	2 - Card set code from scryfall API found at https://api.scryfall.com/sets

	3 - If the card is a foil or not. Use "yes" if the card is a foil.
	```

# Database tables:
	View the SQL class to see database tables and structure. Primary keys on card tables should be card_id or card_id/sequence for tables that contain a sequence.