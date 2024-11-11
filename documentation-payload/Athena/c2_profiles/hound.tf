output "workload_identity_pool" {
  value       = google_iam_workload_identity_pool.bishopfox.name
  description = "Workload Identity Pool ID"
  sensitive   = false
}

output "workload_identity_pool_provider" {
  value       = google_iam_workload_identity_pool_provider.bishopfox.name
  description = "Workload Identity Pool Provider Name"
  sensitive   = false
}

output "service_account" {
  value       = google_service_account.bishopfox.email
  description = "WIF Service Account"
  sensitive   = false
}

# output value of exportet WIF credential config file
output "gcp_wif_config" {
  value = file("gcp-wif-config.json")
}

