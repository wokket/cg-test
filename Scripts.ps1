
# Enable MSI
Update-AzFunctionApp -Name cg-test-funcs `
    -ResourceGroupName wva `
    -IdentityType SystemAssigned

# OR

 az functionapp identity assign -g wva -n cg-test-funcs

