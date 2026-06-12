namespace Hemma.Modules.Property.Errors;

internal static class PropertyErrors
{
    public static readonly ErrorOr.Error ProjectNotFound =
        ErrorOr.Error.NotFound("Property.Project.NotFound", "Project was not found.");

    public static readonly ErrorOr.Error ProjectNameInvalid =
        ErrorOr.Error.Validation("Property.Project.NameInvalid", "Project name is required and cannot exceed 160 characters.");

    public static readonly ErrorOr.Error ProjectDescriptionInvalid =
        ErrorOr.Error.Validation("Property.Project.DescriptionInvalid", "Project description cannot exceed 2,000 characters.");

    public static readonly ErrorOr.Error ProjectAreaInvalid =
        ErrorOr.Error.Validation("Property.Project.AreaInvalid", "Project area cannot exceed 100 characters.");

    public static readonly ErrorOr.Error ProjectNotesInvalid =
        ErrorOr.Error.Validation("Property.Project.NotesInvalid", "Project notes cannot exceed 4,000 characters.");

    public static readonly ErrorOr.Error ProjectDateRangeInvalid =
        ErrorOr.Error.Validation("Property.Project.DateRangeInvalid", "Project target end date cannot be before the target start date.");

    public static readonly ErrorOr.Error ProjectStatusInvalid =
        ErrorOr.Error.Validation("Property.Project.StatusInvalid", "Project status is invalid.");

    public static readonly ErrorOr.Error ProjectPriorityInvalid =
        ErrorOr.Error.Validation("Property.Project.PriorityInvalid", "Project priority is invalid.");

    public static readonly ErrorOr.Error TaskNotFound =
        ErrorOr.Error.NotFound("Property.ProjectTask.NotFound", "Project task was not found.");

    public static readonly ErrorOr.Error TaskTitleInvalid =
        ErrorOr.Error.Validation("Property.ProjectTask.TitleInvalid", "Task title is required and cannot exceed 200 characters.");

    public static readonly ErrorOr.Error TaskStatusInvalid =
        ErrorOr.Error.Validation("Property.ProjectTask.StatusInvalid", "Task status is invalid.");

    public static readonly ErrorOr.Error TaskOrderInvalid =
        ErrorOr.Error.Validation("Property.ProjectTask.OrderInvalid", "Task order must include every current task exactly once.");

    public static readonly ErrorOr.Error LinkNotFound =
        ErrorOr.Error.NotFound("Property.ProjectLink.NotFound", "Project link was not found.");

    public static readonly ErrorOr.Error LinkLabelInvalid =
        ErrorOr.Error.Validation("Property.ProjectLink.LabelInvalid", "Link label is required and cannot exceed 160 characters.");

    public static readonly ErrorOr.Error LinkUrlInvalid =
        ErrorOr.Error.Validation("Property.ProjectLink.UrlInvalid", "Link URL is required and cannot exceed 2,048 characters.");

    public static readonly ErrorOr.Error AttachmentNotFound =
        ErrorOr.Error.NotFound("Property.ProjectAttachment.NotFound", "Project attachment was not found.");

    public static readonly ErrorOr.Error AttachmentFileInvalid =
        ErrorOr.Error.Validation("Property.ProjectAttachment.FileInvalid", "Attachment file must be a supported image or PDF and within the allowed size.");

    public static readonly ErrorOr.Error AttachmentBlobInvalid =
        ErrorOr.Error.Validation("Property.ProjectAttachment.BlobInvalid", "Attachment blob reference is invalid.");

    public static readonly ErrorOr.Error MaintenancePlanNotFound =
        ErrorOr.Error.NotFound("Property.MaintenancePlan.NotFound", "Maintenance plan was not found.");

    public static readonly ErrorOr.Error MaintenancePlanTitleInvalid =
        ErrorOr.Error.Validation("Property.MaintenancePlan.TitleInvalid", "Maintenance plan title is required and cannot exceed 160 characters.");

    public static readonly ErrorOr.Error MaintenancePlanDescriptionInvalid =
        ErrorOr.Error.Validation("Property.MaintenancePlan.DescriptionInvalid", "Maintenance plan description cannot exceed 2,000 characters.");

    public static readonly ErrorOr.Error MaintenancePlanAreaInvalid =
        ErrorOr.Error.Validation("Property.MaintenancePlan.AreaInvalid", "Maintenance plan area cannot exceed 100 characters.");

    public static readonly ErrorOr.Error MaintenanceRecurrenceInvalid =
        ErrorOr.Error.Validation("Property.MaintenancePlan.RecurrenceInvalid", "Maintenance recurrence unit must be Month or Year and the interval must be between 1 and 120.");

    public static readonly ErrorOr.Error MaintenanceLeadTimeInvalid =
        ErrorOr.Error.Validation("Property.MaintenancePlan.LeadTimeInvalid", "Maintenance lead time must be between 0 and 365 days.");

    public static readonly ErrorOr.Error MaintenanceOccurrenceNotFound =
        ErrorOr.Error.NotFound("Property.MaintenanceOccurrence.NotFound", "Maintenance occurrence was not found.");

    public static readonly ErrorOr.Error MaintenanceOccurrenceNotOpen =
        ErrorOr.Error.Validation("Property.MaintenanceOccurrence.NotOpen", "Maintenance occurrence is not upcoming and cannot be changed.");

    public static readonly ErrorOr.Error HistoryEntryNotFound =
        ErrorOr.Error.NotFound("Property.HistoryEntry.NotFound", "History entry was not found.");

    public static readonly ErrorOr.Error HistoryEntryTitleInvalid =
        ErrorOr.Error.Validation("Property.HistoryEntry.TitleInvalid", "History entry title is required and cannot exceed 160 characters.");

    public static readonly ErrorOr.Error HistoryEntryAreaInvalid =
        ErrorOr.Error.Validation("Property.HistoryEntry.AreaInvalid", "History entry area cannot exceed 100 characters.");

    public static readonly ErrorOr.Error HistoryEntryTypeInvalid =
        ErrorOr.Error.Validation("Property.HistoryEntry.TypeInvalid", "History entry type must be Project, Maintenance, or Manual.");

    public static readonly ErrorOr.Error HistoryEntrySourceInvalid =
        ErrorOr.Error.Validation("Property.HistoryEntry.SourceInvalid", "History entry source does not match its type.");

    public static readonly ErrorOr.Error HistoryPhotoNotFound =
        ErrorOr.Error.NotFound("Property.HistoryPhoto.NotFound", "History photo was not found.");

    public static readonly ErrorOr.Error HistoryPhotoInvalid =
        ErrorOr.Error.Validation("Property.HistoryPhoto.Invalid", "History photo must be a supported image and within the allowed size.");

    public static readonly ErrorOr.Error HistoryPhotoBlobInvalid =
        ErrorOr.Error.Validation("Property.HistoryPhoto.BlobInvalid", "History photo blob reference is invalid.");

    public static readonly ErrorOr.Error AreaNotFound =
        ErrorOr.Error.NotFound("Property.Area.NotFound", "Area was not found.");

    public static readonly ErrorOr.Error AreaNameInvalid =
        ErrorOr.Error.Validation("Property.Area.NameInvalid", "Area name is required and cannot exceed 120 characters.");

    public static readonly ErrorOr.Error AreaDescriptionInvalid =
        ErrorOr.Error.Validation("Property.Area.DescriptionInvalid", "Area description cannot exceed 1,000 characters.");

    public static readonly ErrorOr.Error AreaNameAlreadyExists =
        ErrorOr.Error.Conflict("Property.Area.NameAlreadyExists", "An area with this name already exists in the household.");

    public static readonly ErrorOr.Error AreaOrderInvalid =
        ErrorOr.Error.Validation("Property.Area.OrderInvalid", "Area order must include every current area exactly once.");

    public static readonly ErrorOr.Error TagNotFound =
        ErrorOr.Error.NotFound("Property.Tag.NotFound", "Tag was not found.");

    public static readonly ErrorOr.Error TagNameInvalid =
        ErrorOr.Error.Validation("Property.Tag.NameInvalid", "Tag name is required and cannot exceed 80 characters.");

    public static readonly ErrorOr.Error TagColorInvalid =
        ErrorOr.Error.Validation("Property.Tag.ColorInvalid", "Tag color cannot exceed 40 characters.");

    public static readonly ErrorOr.Error TagNameAlreadyExists =
        ErrorOr.Error.Conflict("Property.Tag.NameAlreadyExists", "A tag with this name already exists in the household.");

    public static readonly ErrorOr.Error TagTargetTypeInvalid =
        ErrorOr.Error.Validation("Property.TagAssignment.TargetTypeInvalid", "Tag target type is invalid.");

    public static readonly ErrorOr.Error TagTargetNotFound =
        ErrorOr.Error.NotFound("Property.TagAssignment.TargetNotFound", "Tag assignment target was not found.");

    public static readonly ErrorOr.Error TagAssignmentInvalid =
        ErrorOr.Error.Validation("Property.TagAssignment.Invalid", "All assigned tags must belong to the same household.");
}
