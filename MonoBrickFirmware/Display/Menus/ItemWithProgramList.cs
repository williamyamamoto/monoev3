﻿using System;
using System.Collections.Generic;
using MonoBrickFirmware.FileSystem;
using MonoBrickFirmware.Display.Dialogs;
using System.Threading;

namespace MonoBrickFirmware.Display.Menus
{
	public class ItemWithProgramList : ItemList
	{
		private bool useEscToStop;
		public ItemWithProgramList (string title, bool useEscToStop): base(title, Font.MediumFont)
		{
			this.useEscToStop = useEscToStop;	 		
		}

		protected override List<IChildItem> OnCreateChildList ()
		{
			List<ProgramInformation> programs = ProgramManager.Instance.GetProgramInformationList();
			var childList = new List<IChildItem> ();
			foreach(var program in programs)
			{
				childList.Add (new ProgramDialog (program, useEscToStop));
			}
			return childList;
		}
	}


	internal class ProgramDialog : ItemWithDialog<SelectDialog<string>>, IParentItem
	{
		private ProgramInformation programInformation;
		private bool useEscToStop;
		public ProgramDialog(ProgramInformation programInformation, bool useEscToStop): base(new SelectDialog<string> (new string[] {"Run Program", "Run In AOT", "AOT Compile", "Delete Program"}, "Options", true), programInformation.Name)
		{
			this.programInformation = programInformation;
			this.useEscToStop = useEscToStop;
		}

		public override void OnExit (SelectDialog<string> dialog)
		{
			if (!dialog.EscPressed) {
				switch (dialog.GetSelectionIndex ()) {
				case 0:
					var startDialog = new ExecuteProgramDialog (this.programInformation, false, useEscToStop);
					startDialog.Start (this);
					break;
				case 1:
					if (!programInformation.IsAOTCompiled) 
					{
						var aotDialog = new AotCompileDialog(this.programInformation);
						aotDialog.SetFocus (Parent);
					} 
					else 
					{
						var start = new ExecuteProgramDialog (this.programInformation,true, useEscToStop);
						start.Start (this);
					}
					break;
				case 2:
					if (programInformation.IsAOTCompiled) 
					{
						var aotQuestion = new AotQuestionDialog ();
						aotQuestion.SetFocus (Parent);
					} 
					else 
					{
						var aotDialog = new AotCompileDialog(this.programInformation);
						aotDialog.SetFocus (Parent);
					}
					break;
				case 3:
					var deleteDialog = new DeleteDialog (this.programInformation);
					deleteDialog.SetFocus (Parent);
					break;
				}
			} 
		}

		#region IParentItem implementation

		public void SetFocus (IChildItem item)
		{
			Parent.SetFocus (item);
		}

		public void RemoveFocus (IChildItem item)
		{
			Parent.RemoveFocus (item);
		}

		public void SuspendEvents (IChildItem item)
		{
			Parent.SuspendEvents (item);
		}

		public void ResumeEvents (IChildItem item)
		{
			Parent.ResumeEvents (item);
		}

		#endregion
	}

	internal class ExecuteProgramDialog : IChildItem
	{
		private ProgramInformation program;
		private bool inAot;
		private bool useEscToStop;

		public ExecuteProgramDialog(ProgramInformation programInfo, bool inAot, bool useEscToStop)
		{
			this.program = programInfo;
			this.useEscToStop = useEscToStop;
			this.inAot = inAot;
		}

		private void StartProgramInANewThread()
		{
			if (!program.IsRunning)
			{
				(new Thread (() => {
					if (useEscToStop) 
					{
						Parent.SuspendEvents (this);
					}
					ProgramManager.Instance.StartAndWaitForProgram (program, inAot);
					OnDone ();
				})).Start ();
			} 
			else 
			{
				//Show some dialog
			}
		}


		public void Start(IParentItem parent)
		{
			Parent = parent;
			Parent.SetFocus (this);
			StartProgramInANewThread ();
		}

		#region IChildItem implementation

		public void OnEnterPressed ()
		{
		
		}

		public void OnLeftPressed ()
		{
			
		}

		public void OnRightPressed ()
		{
			
		}

		public void OnUpPressed ()
		{
			
		}

		public void OnDownPressed ()
		{
			
		}

		public void OnEscPressed ()
		{
			ProgramManager.Instance.StopProgram (this.program);		
		}

		public void OnDrawTitle (Font font, Rectangle rectangle, bool selected)
		{
			
		}

		public void OnDrawContent ()
		{
			
		}

		public void OnHideContent ()
		{
			
		}

		private void OnDone()
		{
			if (useEscToStop)
			{
				Parent.ResumeEvents (this);
			}
			Parent.RemoveFocus (this);
		}


		public IParentItem Parent { get; set;}

		#endregion
	}


	internal class AotQuestionDialog : ItemWithDialog<QuestionDialog>
	{
		public AotQuestionDialog(): base(new QuestionDialog ("Progran already compiled. Recompile?", "AOT recompile"),"")
		{

		}

		public override void OnExit (QuestionDialog dialog)
		{
			if (dialog.IsPositiveSelected) 
			{
				//AOTCompileAndShowDialog(false);
			} 
		}
	}

	internal class AotCompileDialog : ItemWithDialog<StepDialog>
	{
		public AotCompileDialog(ProgramInformation programInformation): base(new StepDialog("Compiling", 
			new List<IStep> (){new StepContainer (delegate() {return ProgramManager.Instance.AOTCompileProgram(programInformation);}, 
				"compiling program", "Failed to compile")}),"")
		{
			
		
		}

		public override void OnExit (StepDialog dialog)
		{
			if(!dialog.ExecutedOk)
			{

			}
		}
	}

	internal class DeleteDialog : ItemWithDialog<QuestionDialog>
	{
		private ProgramInformation program;
		public DeleteDialog(ProgramInformation programInformation): base(new QuestionDialog ("Are you sure?", "Delete"), "")
		{
			this.program = programInformation;
		}

		public override void OnExit(QuestionDialog dialog)
		{
			if (dialog.IsPositiveSelected)
			{
				var deleteDialog = new DeleteStepsDialog(this.program);
				deleteDialog.SetFocus(this.Parent);
			}		
		}

		private class DeleteStepsDialog : ItemWithDialog<ProgressDialog>
		{
			public DeleteStepsDialog(ProgramInformation programInformation): base(new ProgressDialog("", new StepContainer (() => {
				ProgramManager.Instance.DeleteProgram (programInformation);
				return true;
			}, "Deleting ", "Error deleting program")),"")
			{

			}

			public override void OnExit (ProgressDialog dialog)
			{

			}

		}

	}
}
