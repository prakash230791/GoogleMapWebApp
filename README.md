# Google Map Web App Deployment Guide

This guide outlines the steps to build, publish, and deploy the Google Map Web App to a Google Cloud Platform (GCP) Virtual Machine (VM) running a Debian/Ubuntu-based Linux distribution.

## Table of Contents
1.  [Prerequisites](#1-prerequisites)
2.  [Local Setup and Build](#2-local-setup-and-build)
3.  [Google Cloud VM Setup](#3-google-cloud-vm-setup)
4.  [Deployment using WinSCP](#4-deployment-using-winSCP)
5.  [Service Configuration and Start](#5-service-configuration-and-start)
6.  [Google Cloud Firewall Configuration](#6-google-cloud-firewall-configuration)
7.  [Accessing the Application](#7-accessing-the-application)

---

## 1. Prerequisites

### Local Machine
*   .NET SDK (version 9.0 or later)
*   WinSCP (or any SCP client)

### Google Cloud VM
*   A running Google Cloud VM instance (e.g., Debian, Ubuntu)
*   SSH access to the VM

---

## 2. Local Setup and Build

1.  **Navigate to the project directory:**
    Open your terminal or command prompt and go to the root of your project where `GoogleMapWebApp.csproj` is located.
    ```bash
    cd c:\Users\praka\OneDrive\Documents\AI\Lab\Google\Lab04-WebApp
    ```

2.  **Publish the application:**
    This command compiles your application and prepares it for deployment in a `publish` folder.
    ```bash
    dotnet publish GoogleMapWebApp\GoogleMapWebApp.csproj -c Release -o GoogleMapWebApp\publish
    ```
    This will create a `publish` directory inside `GoogleMapWebApp` containing all the necessary deployment files.

---

## 3. Google Cloud VM Setup

### 3.1. Install .NET Runtime on the VM

Connect to your Google Cloud VM via SSH and run the following commands to install the ASP.NET Core 9.0 Runtime.

```bash
# Download and register the Microsoft package repository
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Update your package list and install the ASP.NET Core 9.0 runtime
sudo apt-get update
sudo apt-get install -y aspnetcore-runtime-9.0
```

### 3.2. Create Application Directory and Set Permissions

Create a directory on your VM where your application files will reside and set the correct permissions. Replace `gemini-user` with your actual SSH username for the VM.

```bash
sudo mkdir -p /var/www/googlemapwebapp
sudo chown gemini-user:gemini-user /var/www/googlemapwebapp
```

---

## 4. Deployment using WinSCP

Now, transfer your published application files and the service file to the VM using WinSCP.

1.  **Open WinSCP** and connect to your Google Cloud VM using its external IP address and your SSH credentials (e.g., private key).
2.  **Navigate Local Directory:** In the left panel (your local computer), navigate to the published application files:
    `c:\Users\praka\OneDrive\Documents\AI\Lab\Google\Lab04-WebApp\GoogleMapWebApp\publish`
3.  **Navigate Remote Directory:** In the right panel (your VM), navigate to the application directory you created:
    `/var/www/googlemapwebapp/`
4.  **Transfer Application Files:** Select all the files and folders from your local `publish` directory (left panel) and drag them to the `/var/www/googlemapwebapp/` directory on the VM (right panel).
5.  **Transfer Service File:** Locate your `googlemapwebapp.service` file locally at `c:\Users\praka\OneDrive\Documents\AI\Lab\Google\Lab04-WebApp\GoogleMapWebApp\googlemapwebapp.service`. Drag this file to the `/var/www/googlemapwebapp/` directory on the VM as well.

---

## 5. Service Configuration and Start

After transferring the files, you need to configure and start the systemd service on your VM.

1.  **Connect to your VM via SSH.**

2.  **Move the service file to the systemd directory:**
    ```bash
    sudo mv /var/www/googlemapwebapp/googlemapwebapp.service /etc/systemd/system/
    ```

3.  **Edit the service file:**
    Open the service file to ensure it's configured correctly to run your application and listen on the correct network interfaces.
    ```bash
    sudo nano /etc/systemd/system/googlemapwebapp.service
    ```
    Replace the entire content with the following:
    ```ini
    [Unit]
    Description=Google Map Web App

    [Service]
    WorkingDirectory=/var/www/googlemapwebapp
    ExecStart=/usr/bin/dotnet /var/www/googlemapwebapp/GoogleMapWebApp.dll
    Restart=always
    RestartSec=10
    KillSignal=SIGINT
    SyslogIdentifier=googlemapwebapp
    User=gemini-user # Ensure this matches your VM's SSH username
    Environment=ASPNETCORE_ENVIRONMENT=Production
    Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false
    Environment=ASPNETCORE_URLS=http://*:5000 # This makes the app listen on all interfaces

    [Install]
    WantedBy=multi-user.target
    ```
    Save the file and exit `nano` (`Ctrl+X`, then `Y`, then `Enter`).

4.  **Reload systemd, enable, and start the service:**
    ```bash
    sudo systemctl daemon-reload
    sudo systemctl enable googlemapwebapp.service
    sudo systemctl start googlemapwebapp.service
    ```

5.  **Check the service status:**
    ```bash
    sudo systemctl status googlemapwebapp.service
    ```
    You should see `Active: active (running)`.

---

## 6. Google Cloud Firewall Configuration

To access your application from the internet, you must create a firewall rule in your Google Cloud project.

1.  **Go to the Google Cloud Console:** [https://console.cloud.google.com/](https://console.cloud.google.com/)
2.  **Navigate to Firewall Rules:** In the navigation menu (☰), go to **VPC network** > **Firewall**.
3.  **Create a New Firewall Rule:** Click the **CREATE FIREWALL RULE** button.
4.  **Configure the Rule:**
    *   **Name:** `allow-port-5000` (or any descriptive name)
    *   **Network:** Select the network your VM is in (usually `default`).
    *   **Direction of traffic:** `Ingress`
    *   **Action on match:** `Allow`
    *   **Targets:** `All instances in the network` (or specify target tags if you use them).
    *   **Source filter:** `IPv4 ranges`
    *   **Source IPv4 ranges:** `0.0.0.0/0` (to allow access from anywhere; for production, consider restricting to specific IPs).
    *   **Protocols and ports:**
        *   Select `Specified protocols and ports`.
        *   Check `TCP` and enter `5000` in the text box.
5.  **Save the Rule:** Click **Create**.

---

## 7. Accessing the Application

Once all the above steps are completed, you can access your application from any web browser using your VM's external IP address and port 5000.

Example URL: `http://34.23.161.59:5000` (Replace `34.23.161.59` with your VM's actual external IP address).

## 8. Configuring Custom Domain (myappss.top)

To access your application using your custom domain `myappss.top`, you need to configure DNS records with your domain registrar (GoDaddy).

1.  **Log in to your GoDaddy account.**
2.  **Navigate to your DNS management page** for `myappss.top`.
3.  **Add or modify an A record:**
    *   **Type:** `A`
    *   **Name:** `@` (or leave blank for the root domain)
    *   **Value:** Your Google Cloud VM's external IP address (e.g., `34.23.161.59`)
    *   **TTL:** (Time To Live) - You can leave this as default or set it to a lower value (e.g., 600 seconds) for faster propagation.
4.  **Save the changes.**

DNS changes can take some time to propagate across the internet (up to 48 hours, but often faster). Once propagated, you should be able to access your application using `http://myappss.top:5000`.

**Note on Port 5000:** To access your application directly via `http://myappss.top` (without specifying port 5000), you would typically set up a reverse proxy (like Nginx or Apache) on your VM to forward requests from port 80 (HTTP) to port 5000. This is an advanced configuration not covered in this guide.

## 9. Understanding Google Cloud VM Deletion

Google Cloud VMs can be deleted for several reasons:

*   **Manual Deletion:** You or another authorized user manually deleted the VM instance.
*   **Preemptible VMs:** If you created a "preemptible VM," Google Cloud can terminate it at any time (usually within 24 hours) if it needs the resources. These VMs are cheaper but not suitable for long-running or critical workloads.
*   **Instance Group Manager (IGM):** If your VM was part of an Instance Group Manager, the IGM might have scaled down the group, leading to the deletion of instances.
*   **Billing Issues:** If your Google Cloud account has billing issues (e.g., expired credit card, spending limit reached), resources might be suspended or deleted.

Always ensure your VMs are configured as standard (non-preemptible) instances for persistent workloads and monitor your billing.

## 10. VM Naming Convention

The request to change the VM name suffix to `vm02` is noted. This is an operational task performed within the Google Cloud Console or via `gcloud` CLI commands and does not directly impact the application's code or deployment process described in this guide. You would typically stop the VM, rename it, and then start it again.