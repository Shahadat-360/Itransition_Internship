# Setting Up Support Ticket System with Google Drive and Power Automate

This document provides detailed instructions for setting up the FormsApp support ticket system to work with Google Drive and Power Automate.

## Part 1: Google Drive Configuration

### 1. Create a Google Drive Folder
1. Sign in to your Google Drive account
2. Create a new folder named "SupportTickets"
3. Note the folder ID from the URL (it's the long alphanumeric string after /folders/ in the URL)

### 2. Update Application Settings
1. Open your `appsettings.json` file
2. Update the GoogleDrive section:
   ```json
   "GoogleDrive": {
     "ApiKey": "AIzaSyAKIroeZKAU6FXXCsldH0VTR8rdNIMGfLw",
     "FolderId": "[YOUR_FOLDER_ID]",
     "UploadEndpoint": "https://www.googleapis.com/upload/drive/v3/files"
   }
   ```
3. Replace `[YOUR_FOLDER_ID]` with the actual folder ID from step 1

## Part 2: Power Automate Flow Setup

### 1. Create a New Automated Flow
1. Sign in to [Power Automate](https://flow.microsoft.com/)
2. Click on "Create" > "Automated cloud flow"
3. Name your flow "FormsApp Support Ticket Notification"
4. For the trigger, select "When a file is created (properties only)" from the Google Drive connector
5. Click "Create"

### 2. Configure the Google Drive Trigger
1. Sign in to your Google account when prompted
2. In the "Folder" field, select the "SupportTickets" folder
3. Click "Show advanced options" and set:
   - Include subfolders: No
   - Filter query: leave blank or set to `mimeType='application/json'`

### 3. Add a Get File Content Step
1. Click "+ New step"
2. Search for "Google Drive" and select "Get file content"
3. For the File ID field, click on the dynamic content button and select "ID" from the trigger output

### 4. Add a Compose Step to Decode the Content
1. Click "+ New step"
2. Search for "Compose" and select it
3. In the Inputs field, enter this expression:
   ```
   @{base64ToString(body('Get_file_content').$content)}
   ```
4. This decodes the base64-encoded file content

### 5. Add a Parse JSON Step
1. Click "+ New step"
2. Search for "Parse JSON" and select it
3. For the Content field, select the output from the Compose step
4. For the Schema field, click "Generate from sample" and paste this sample:
   ```json
   {
     "Id": 123,
     "ReportedBy": "user@example.com",
     "Summary": "Cannot submit form responses",
     "Template": "Customer Feedback Survey",
     "Link": "https://formsapp.com/FormTemplate/Details/42",
     "Priority": "High",
     "CreatedAt": "2023-08-15T14:30:00Z"
   }
   ```

### 6. Add a Gmail Notification Step
1. Click "+ New step"
2. Search for "Gmail" and select "Send email (V2)"
3. Connect your Gmail account if not already connected
4. Configure the email:
   - To: [your admin email]
   - Subject: `New Support Ticket: @{body('Parse_JSON').Priority}`
   - Body:
     ```html
     <h2>Support Ticket Notification</h2>
     <p><strong>Summary:</strong> @{body('Parse_JSON').Summary}</p>
     <p><strong>Reported By:</strong> @{body('Parse_JSON').ReportedBy}</p>
     <p><strong>Priority:</strong> @{body('Parse_JSON').Priority}</p>
     <p><strong>Template:</strong> @{body('Parse_JSON').Template}</p>
     <p><strong>Link:</strong> <a href="@{body('Parse_JSON').Link}">@{body('Parse_JSON').Link}</a></p>
     <p><strong>Created At:</strong> @{body('Parse_JSON').CreatedAt}</p>
     ```

### 7. Add a Mobile Notification Step
1. Click "+ New step"
2. Search for "Notifications" and select "Send a mobile notification"
3. Configure the notification:
   - Text: `New Support Ticket: @{body('Parse_JSON').Summary}`
   - Link URL: `@{body('Parse_JSON').Link}`
   - Link Text: "View Details"

### 8. Save and Test the Flow
1. Click "Save" at the top right
2. Click "Test" to manually test the flow
3. Or submit a support ticket from your application to trigger it automatically

## Part 3: Testing the Integration

### 1. From Application
1. Log in to the FormsApp
2. Navigate to any page
3. Click the Help icon in the navigation bar or the "Create support ticket" link in the footer
4. Fill out the support ticket form and submit
5. Check that you receive both an email and a mobile notification

### 2. Manual Testing with JSON File
You can also test by manually uploading a JSON file to the Google Drive folder:

1. Create a file named `test_ticket.json` with this content:
   ```json
   {
     "Id": 999,
     "ReportedBy": "test.user@example.com",
     "Summary": "Test support ticket",
     "Template": "Test Template",
     "Link": "https://formsapp.com/test",
     "Priority": "High",
     "CreatedAt": "2023-08-15T14:30:00Z"
   }
   ```
2. Upload it to your SupportTickets folder in Google Drive
3. Check for the email and mobile notification

## Troubleshooting

### Flow Not Triggering
- Verify the Google Drive folder ID is correct
- Check if your Power Automate account has proper permissions to the folder
- Make sure the flow is turned on

### Content Parsing Issues
- If Parse JSON fails, check the output of the Compose step
- Verify the JSON structure matches the expected schema
- Test with a simple, valid JSON file

### Google Drive API Problems
- Check your API key is valid and has proper permissions
- Verify the folder ID is correct
- Look at the application logs for detailed error messages

## Security Considerations

- Your API key provides access to your Google Drive account. Keep it secure.
- Consider implementing OAuth 2.0 for a more secure authentication method
- In a production environment, always use HTTPS for all API calls 