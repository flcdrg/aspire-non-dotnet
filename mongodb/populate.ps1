Import-Module Mdbc

$products = @(
	[ordered]@{
		"id" = 'chicken-coop-cleaner'
		"name" = 'Cozy Coop Cleaner'
		"description" = 'Keep your hens happy with a lavender-scented, pet-safe coop spray.'
		"price" = 14.99
		"imageUrl" = 'https://images.unsplash.com/photo-1573333744619-00d101e99133??auto=format&fit=crop&w=600&q=80'
		"category" = 'Chickens'
	}
	[ordered]@{
		"id" = 'turtle-terrarium-kit'
		"name" = 'Lagoon Terrarium Starter Kit'
		"description" = 'All-in-one habitat kit for small turtles with basking dock and LED lighting.'
		"price" = 89.5
		"imageUrl" = 'https://images.unsplash.com/photo-1663907181190-6ed43256458d?auto=format&fit=crop&w=600&q=80'
		"category" = 'Turtles'
	}
	[ordered]@{
		"id" = 'catnip-toy-set'
		"name" = 'Feline Fiesta Catnip Toys'
		"description" = 'A trio of hand-stitched toys packed with organic catnip.'
		"price" = 22.0
		"imageUrl" = 'https://images.unsplash.com/photo-1518791841217-8f162f1e1131?auto=format&fit=crop&w=600&q=80'
		"category" = 'Cats'
	}
	[ordered]@{
		"id" = 'guinea-pig-salad'
		"name" = 'Garden Greens Salad Mix'
		"description" = 'Dried chamomile, carrot curls, and rose hips for guinea pigs and rabbits.'
		"price" = 11.75
		"imageUrl" = 'https://images.unsplash.com/photo-1612267168669-679c961c5b31?auto=format&fit=crop&w=600&q=80'
		"category" = 'Small Pets'
	}
	[ordered]@{
		"id" = 'dog-spa-shampoo'
		"name" = 'Tail Waggers Spa Shampoo'
		"description" = 'Oatmeal and aloe shampoo that soothes dry skin and keeps coats shiny.'
		"price" = 18.25
		"imageUrl" = 'https://images.unsplash.com/photo-1518717758536-85ae29035b6d?auto=format&fit=crop&w=600&q=80'
		"category" = 'Dogs'
	}
	[ordered]@{
		"id" = 'parakeet-playground'
		"name" = 'Skyline Play Tower'
		"description" = 'Colorful perches and bells designed to keep parakeets entertained for hours.'
		"price" = 32.4
		"imageUrl" = 'https://images.unsplash.com/photo-1652536122320-ca870caea2ae?auto=format&fit=crop&w=600&q=80'
		"category" = 'Birds'
	}
)

Connect-Mdbc . petstore products -NewCollection
Add-MdbcData -InputObject $products

