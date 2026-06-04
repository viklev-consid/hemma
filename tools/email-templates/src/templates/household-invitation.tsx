import type { EmailTemplateDefinition } from "../template-contract";
import { ActionLink, Paragraph, Strong, TemplateShell, TokenCode } from "./template-shell";

function HouseholdInvitation() {
  return (
    <TemplateShell preview="You're invited to an household" title="Household invitation">
      <Paragraph>
        You've been invited to join an household as <Strong>{"{{role}}"}</Strong>.
      </Paragraph>
      <ActionLink href="{{invitationUrl}}">Accept household invitation</ActionLink>
      <Paragraph>If the link does not work, copy this token into the invitation screen:</Paragraph>
      <Paragraph>
        <TokenCode>{"{{token}}"}</TokenCode>
      </Paragraph>
      <Paragraph>If you did not expect this invitation, you can ignore this email.</Paragraph>
    </TemplateShell>
  );
}

export const householdInvitationTemplate: EmailTemplateDefinition = {
  id: "households.invitation",
  subject: "You're invited to an household",
  variables: {
    invitationUrl: { type: "url", required: true },
    role: { type: "string", required: true },
    token: { type: "string", required: true },
  },
  render: HouseholdInvitation,
};
