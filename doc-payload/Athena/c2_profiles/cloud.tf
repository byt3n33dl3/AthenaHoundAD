
resource "google_iam_workload_identity_pool" "bishopfox" {
  description               = "managed by terraform"
  disabled                  = false
  display_name              = var.displayName
  project                   = var.projectID
  workload_identity_pool_id = var.poolID
  timeouts {
    create = null
    delete = null
    update = null
  }
}

resource "google_iam_workload_identity_pool_provider" "bishopfox" {
  attribute_condition = null
  attribute_mapping = {
    "attribute.account"  = "assertion.account"
    "attribute.aws_role" = "assertion.arn.extract('assumed-role/{role}/')"
    "google.subject"     = "assertion.arn"
  }
  description                        = "managed by terraform"
  disabled                           = false
  display_name                       = null
  project                            = var.projectID
  workload_identity_pool_id          = var.poolID
  workload_identity_pool_provider_id = var.providerID
  aws {
    account_id = var.AWS_accountID
  }
  timeouts {
    create = null
    delete = null
    update = null
  }
  depends_on = [google_iam_workload_identity_pool.bishopfox]
}

resource "google_service_account" "bishopfox" {
  account_id   = var.serviceAccountID
  description  = "managed by terraform"
  disabled     = false
  display_name = var.serviceAccountDisplayName
  project      = var.projectID
  timeouts {
    create = null
  }
}

resource "google_service_account_iam_policy" "bishopfox" {
  policy_data        = "{\"bindings\":[{\"members\":[\"principalSet://iam.googleapis.com/projects/${var.projectNumber}/locations/global/workloadIdentityPools/${var.poolID}/*\",\"principalSet://iam.googleapis.com/projects/${var.projectNumber}/locations/global/workloadIdentityPools/${var.poolID}/attribute.aws_role/${var.AWS_iamRole1}\",\"principalSet://iam.googleapis.com/projects/${var.projectNumber}/locations/global/workloadIdentityPools/${var.poolID}/attribute.aws_role/${var.AWS_iamRole2}\"],\"role\":\"roles/iam.workloadIdentityUser\"}]}"
  service_account_id = google_service_account.bishopfox.id
  depends_on         = [google_service_account.bishopfox]
}

resource "null_resource" "bishopfox" {
  provisioner "local-exec" {
    command = "gcloud iam workload-identity-pools create-cred-config projects/${var.projectNumber}/locations/global/workloadIdentityPools/${var.poolID}/providers/${var.providerID} --service-account=${google_service_account.bishopfox.email} --aws --enable-imdsv2 --output-file=gcp-wif-config.json --quiet"
  }
  depends_on = [google_iam_workload_identity_pool_provider.bishopfox]
}









