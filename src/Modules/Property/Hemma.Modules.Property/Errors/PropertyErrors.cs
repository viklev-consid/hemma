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
}
