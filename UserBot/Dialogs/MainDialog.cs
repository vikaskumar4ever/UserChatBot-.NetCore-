using System;
using System.Linq;
using AdaptiveCards;
using Newtonsoft.Json;
using System.Threading;
using Newtonsoft.Json.Linq;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Bot.Builder.Dialogs;
using System.Text.RegularExpressions;
using Microsoft.Bot.Builder.Dialogs.Choices;

namespace UserBot.Bots
{
    public class MainDialog : ComponentDialog
    {
        protected readonly ILogger Logger;
        private readonly string EmailDialogID = "EmailDlg";
        private readonly string PhoneDialogID = "PhoneDlg";
        private readonly string SocialMediaProfileDialogID = "SocialMediaProfileDlg";
        public string SocialMediaProfile { get; set; }

        // Dependency injection uses this constructor to instantiate MainDialog
        public MainDialog(ILogger<MainDialog> logger)
            : base(nameof(MainDialog))
        {

            Logger = logger;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new TextPrompt(EmailDialogID, EmailValidation));
            AddDialog(new TextPrompt(PhoneDialogID, PhoneValidation));
            AddDialog(new TextPrompt(SocialMediaProfileDialogID, SocialMediaProfileValidation));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                UserNameStepAsync,
                UserEmailStepAsync,
                UserPhoneStepAsync,
                SocialMediaProfileStepAsync,
                SocialMediaProfileLinkStepAsync,
                TravelStepAsync,
                TravelPreferenceStepAsync,
                BookReadingStepAsync,
                BookReadingPreferenceStepAsync,
                BookReadingGenreStepAsync,
                MoreFocussedStepAsync,
                MoreInformationStepAsync,
                OtherInformationStepAsync,
                ConfirmStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> UserNameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text("Hello there, Please enter your name.")
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> UserEmailStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["UserName"] = (string)stepContext.Result;
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Hi {(string)stepContext.Values["UserName"]}, welcome to User Details Bot."), cancellationToken);
            return await stepContext.PromptAsync(EmailDialogID, new PromptOptions
            {
                Prompt = MessageFactory.Text("Please enter your Email.")
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> UserPhoneStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["UserEmail"] = (string)stepContext.Result;
            return await stepContext.PromptAsync(PhoneDialogID, new PromptOptions
            {
                Prompt = MessageFactory.Text("Please enter your Phone number.")
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> SocialMediaProfileStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["UserPhone"] = (string)stepContext.Result;
            List<string> socialMediaList = new List<string> { "Instagram", "Snapchat", "Facebook", "Twitter", "LinkedIn" };
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Please select the Social Media Profile you use."), cancellationToken);
            return await CreateAdaptiveCardAsync(socialMediaList, stepContext, cancellationToken);
        }

        private async Task<DialogTurnResult> SocialMediaProfileLinkStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["SocialMediaProfile"] = ((FoundChoice)stepContext.Result).Value;
            SocialMediaProfile = (string)stepContext.Values["SocialMediaProfile"];
            return await stepContext.PromptAsync(SocialMediaProfileDialogID, new PromptOptions
            {
                Prompt = MessageFactory.Text($"Please enter the link of your {SocialMediaProfile} social media profile.")
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> TravelStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["SocialMediaProfileLink"] = (string)stepContext.Result;
            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text("Do you like Travelling?")
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> TravelPreferenceStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                stepContext.Values["Travel"] = "Yes";
                List<string> travelPreferenceList = new List<string> { "Solo", "Group" };
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("How do you like to travel?"), cancellationToken);
                return await CreateAdaptiveCardAsync(travelPreferenceList, stepContext, cancellationToken);
            }
            else
            {
                stepContext.Values["Travel"] = "No";
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Sad to hear that you don't like travel. You should definitely go on a trip atleast once in a month."), cancellationToken);
                return await stepContext.NextAsync(null, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> BookReadingStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Values["Travel"].Equals("Yes"))
            {
                stepContext.Values["TravelPreference"] = ((FoundChoice)stepContext.Result).Value;
            }
            else
            {
                stepContext.Values["TravelPreference"] = "No Travel Preference";
            }
            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text("Do you like Reading Books?")
            }, cancellationToken);

        }

        private async Task<DialogTurnResult> BookReadingPreferenceStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                stepContext.Values["BookReading"] = "Yes";
                List<string> bookPreferenceList = new List<string> { "Paperbook", "Audio Book" };
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("How do you like to read books?"), cancellationToken);
                return await CreateAdaptiveCardAsync(bookPreferenceList, stepContext, cancellationToken);
            }
            else
            {
                stepContext.Values["BookReading"] = "No";
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("No problem, you will love reading books when you try reading the Shiva Trilogy by Amish Tripathi"), cancellationToken);
                return await stepContext.NextAsync(null, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> BookReadingGenreStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Values["BookReading"].Equals("Yes"))
            {
                stepContext.Values["BookReadingPreference"] = ((FoundChoice)stepContext.Result).Value;

                if (stepContext.Values["BookReadingPreference"].Equals("Paperbook"))
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Even I don't like listening audio books."), cancellationToken);
                }
                List<string> bookGenreList = new List<string> { "Fiction", "Non Fiction" };
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("What type of books you like to read?"), cancellationToken);
                return await CreateAdaptiveCardAsync(bookGenreList, stepContext, cancellationToken);
            }
            else
            {
                stepContext.Values["BookReadingPreference"] = "No Reading Preference";
                return await stepContext.NextAsync(null, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> MoreFocussedStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Values["BookReading"].Equals("Yes"))
            {
                stepContext.Values["BookReadingGenre"] = ((FoundChoice)stepContext.Result).Value;
            }
            else
            {
                stepContext.Values["BookReadingGenre"] = "Not Reading Books";
            }
            List<string> moreFocussedList = new List<string> { "During Day Time", "During Night Time" };
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("When are you more focussed?"), cancellationToken);
            return await CreateAdaptiveCardAsync(moreFocussedList, stepContext, cancellationToken);
        }

        private async Task<DialogTurnResult> MoreInformationStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["MoreFocussed"] = ((FoundChoice)stepContext.Result).Value;
            if (stepContext.Values["MoreFocussed"].Equals("During Night Time"))
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Even I like Night time. It will be very calm and silent"), cancellationToken);
            }
            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text("Would you like to give any other information?")
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> OtherInformationStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                stepContext.Values["MoreInformation"] = "Yes";
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please give any other information you want me to know about you.")
                }, cancellationToken);
            }
            else
            {
                stepContext.Values["MoreInformation"] = "No";
                return await stepContext.NextAsync(null, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Values["MoreInformation"].Equals("Yes"))
            {
                stepContext.Values["OtherInformation"] = (string)stepContext.Result;
            }
            else
            {
                stepContext.Values["OtherInformation"] = "Not Given";
            }
            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text("Would you like to Confirm your details?")
            }, cancellationToken);
        }


        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                UserProfile userProfile = new UserProfile()
                {
                    UserName = (string)stepContext.Values["UserName"],
                    Email = (string)stepContext.Values["UserEmail"],
                    Phone = (string)stepContext.Values["UserPhone"],
                    SocialMediaProfile = (string)stepContext.Values["SocialMediaProfile"],
                    SocialMediaProfileLink = (string)stepContext.Values["SocialMediaProfileLink"],
                    Travel = (string)stepContext.Values["Travel"],
                    TravelPreference = (string)stepContext.Values["TravelPreference"],
                    BookReading = (string)stepContext.Values["BookReading"],
                    BookReadingPreference = (string)stepContext.Values["BookReadingPreference"],
                    BookReadingGenre = (string)stepContext.Values["BookReadingGenre"],
                    MoreFocussed = (string)stepContext.Values["MoreFocussed"],
                    MoreInformation = (string)stepContext.Values["MoreInformation"],
                    OtherInformation = (string)stepContext.Values["OtherInformation"]
                };

                //Use this JSON for your requirement.
                string stringjson = JsonConvert.SerializeObject(userProfile);

                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Your details are stored, Thank you."), cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Your details are not saved. Thank you."), cancellationToken);
            }
            // Restart the main dialog with a different message the second time around
            var promptMessage = "What else can I do for you?";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
        }

        private async Task<bool> EmailValidation(PromptValidatorContext<string> promptcontext, CancellationToken cancellationtoken)
        {
            string email = promptcontext.Recognized.Value;

            if (string.IsNullOrWhiteSpace(email))
            {
                await promptcontext.Context.SendActivityAsync("The email you entered is not valid, please enter a valid email.", cancellationToken: cancellationtoken);
                return false;
            }

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                if (addr.Address == email)
                {
                    return true;
                }
                else
                {
                    await promptcontext.Context.SendActivityAsync("The email you entered is not valid, please enter a valid email.", cancellationToken: cancellationtoken);
                    return false;
                }
            }
            catch
            {
                await promptcontext.Context.SendActivityAsync("The email you entered is not valid, please enter a valid email.", cancellationToken: cancellationtoken);
                return false;
            }
        }

        private async Task<bool> PhoneValidation(PromptValidatorContext<string> promptcontext, CancellationToken cancellationtoken)
        {
            string number = promptcontext.Recognized.Value;
            if (Regex.IsMatch(number, @"^\d+$"))
            {
                int count = promptcontext.Recognized.Value.Length;
                if (count != 10)
                {
                    await promptcontext.Context.SendActivityAsync("Hello, you are missing some number !!!",
                        cancellationToken: cancellationtoken);
                    return false;
                }
                return true;
            }
            await promptcontext.Context.SendActivityAsync("The phone number is not valid. Please enter a valid number.",
                        cancellationToken: cancellationtoken);
            return false;
        }

        private async Task<bool> SocialMediaProfileValidation(PromptValidatorContext<string> promptcontext, CancellationToken cancellationtoken)
        {
            string profileLink = promptcontext.Recognized.Value;

            if (profileLink.ToLower().Contains(SocialMediaProfile.ToLower()))
            {
                return true;
            }
            else
            {
                await promptcontext.Context.SendActivityAsync($"The profile link you entered for {SocialMediaProfile} is not valid. Please enter correct profile link.", cancellationToken: cancellationtoken);
                return false;
            }

        }

        private async Task<DialogTurnResult> CreateAdaptiveCardAsync(List<string> listOfOptions, WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
            {
                // Use LINQ to turn the choices into submit actions
                Actions = listOfOptions.Select(choice => new AdaptiveSubmitAction
                {
                    Title = choice,
                    Data = choice,  // This will be a string
                }).ToList<AdaptiveAction>(),
            };
            // Prompt
            return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
            {
                Prompt = (Activity)MessageFactory.Attachment(new Attachment
                {
                    ContentType = AdaptiveCard.ContentType,
                    // Convert the AdaptiveCard to a JObject
                    Content = JObject.FromObject(card),
                }),
                Choices = ChoiceFactory.ToChoices(listOfOptions),
                // Don't render the choices outside the card
                Style = ListStyle.None,
            },
                cancellationToken);
        }
    }
}
